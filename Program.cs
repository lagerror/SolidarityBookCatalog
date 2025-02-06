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

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
   
    .CreateLogger();

builder.Host.UseSerilog(); // 使用Serilog作为日志提供者

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
});

builder.Services.AddControllers();
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

//注意以下三者顺序
app.UseAuthentication();
// 添加日志上下文Enricher
app.Use(async (context, next) =>
{
    LogContext.PushProperty("IP", context.Connection.RemoteIpAddress?.ToString());
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