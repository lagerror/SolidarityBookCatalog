using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Serilog;
using Serilog.Context;
using Serilog.Sinks.MongoDB;
using System.Security.Principal;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
   
    .CreateLogger();

builder.Host.UseSerilog(); // 使用Serilog作为日志提供者
//
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
});
// 添加身份验证服务并配置 JWT Bearer 认证
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new RsaSecurityKey(GetRsaPublicKey()) // 使用公钥验证
    };
});

// 添加授权策略
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrManager", policy =>
        policy.RequireRole("admin", "manager"));
    options.AddPolicy("ManagerOrReader", policy =>
        policy.RequireRole("admin, manager","reader"));
});

builder.Services.AddControllers();
// 配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.AllowAnyOrigin()
                //.WithOrigins("http://localhost:5173") // 允许的域名
               .AllowAnyHeader()                 // 允许任何头
               .AllowAnyMethod();                // 允许任何方法
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "图书馆共建图书目录测试", Version = "v1" });
});

// 自定义服务
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ElasticService>();
builder.Services.AddSingleton<ElasticNestService>();
builder.Services.AddSingleton<BibliosService>();
builder.Services.AddSingleton<HoldingService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<ReaderService>();
builder.Services.AddSingleton<LoanWorkService>();
builder.Services.AddSingleton<IsdlLoanService>();
builder.Services.AddSingleton<ToolService>();
builder.Services.AddSingleton<FiscoService>();
builder.Services.AddSingleton<MinioService>();


// 添加内存缓存
builder.Services.AddMemoryCache();
// 注册微信 Token 服务（单例）
builder.Services.AddSingleton<IWeChatService, WeChatService>();
//注册百度IOT服务
builder.Services.AddSingleton<IBDIot,BDIot>();

// 全局共享 MongoDB 连接
var client = new MongoClient(builder.Configuration.GetValue<string>("ConnectionStrings:BookDb"));
builder.Services.AddSingleton<IMongoClient>(client);

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint($"v1/swagger.json", "图书馆共建图书目录测试");
    });
}

app.UseStaticFiles();
// 使用CORS中间件
app.UseCors("AllowSpecificOrigin");
//注意以下三者顺序
app.UseAuthentication();
// 添加日志上下文Enricher
app.Use(async (context, next) =>
{
    // 尝试从X-Forwarded-For头中获取IP地址
    var forwardedFor = context.Request.Headers["X-Forwarded-For"];
    string ip = null;
    if (!StringValues.IsNullOrEmpty(forwardedFor))
    {
        // X-Forwarded-For头可能包含多个IP地址，第一个是客户端的真实IP
        ip = forwardedFor.ToString().Split(',')[0].Trim();
    }
    else
    {
        // 如果X-Forwarded-For头不存在，尝试从X-Real-IP头获取
        var realIp = context.Request.Headers["X-Real-IP"];
        if (!StringValues.IsNullOrEmpty(realIp))
        {
            ip = realIp.ToString();
        }
    }

    // 如果头中都没有找到IP地址，使用连接的RemoteIpAddress
    if (string.IsNullOrEmpty(ip) && context.Connection.RemoteIpAddress != null)
    {
        ip = context.Connection.RemoteIpAddress.ToString();
    }

    // 如果IP地址仍然是本地地址，可以进一步处理或记录
    if (ip == "127.0.0.1" || ip == "::1")
    {
        // 可以根据需要进行特殊处理
    }

    // 添加到日志上下文
    LogContext.PushProperty("IP", ip ?? "Unknown");
    LogContext.PushProperty("User", context.User.Identity.Name ?? "Anonymous");
    
    await next();
});
// 使用 Serilog 替换默认的日志系统
app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

app.Run();

 RSA GetRsaPublicKey()
{
    //不能释放rsa或者使用static，但注意配置文件读取
    var rsa = RSA.Create();
    
        string db = builder.Configuration["ConnectionStrings:BookDb"];
        var client = new MongoClient(db);
        var database = client.GetDatabase("BookReShare");
        var _users = database.GetCollection<User>("user");
        var publicKeyPem = _users.Find(x => x.Username == "jwtToken").FirstOrDefault()?.PublicPem;
        client.Dispose();
        if (string.IsNullOrEmpty(publicKeyPem))
        {
            throw new Exception("Public key not found in database.");
        }

        rsa.ImportFromPem(publicKeyPem); // 导入 PEM 格式的公钥
        return rsa;
    
}