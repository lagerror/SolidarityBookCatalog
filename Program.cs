using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using SolidarityBookCatalog.Models;
using SolidarityBookCatalog.Services;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);



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

    //options.Events = new JwtBearerEvents   //因为包的兼容性，删除Microsoft.IdentityModel.Tokens包即可
    //{
    //    OnAuthenticationFailed = context =>
    //    {
    //        // 获取请求中的 Authorization 头
    //        var authorizationHeader = context.Request.Headers["Authorization"];

    //        // 打印或记录整个 Authorization 头
    //        // 注意：这可能包含敏感信息，确保只在安全的环境中这样做
    //        Console.WriteLine("Authorization Header: " + authorizationHeader);

    //        Console.WriteLine("Authentication failed: " + context.Exception.Message);
    //        return Task.CompletedTask;
    //    },
    //    OnTokenValidated = context =>
    //    {
    //        // 获取请求中的 Authorization 头
    //        var authorizationHeader = context.Request.Headers["Authorization"];

    //        // 打印或记录整个 Authorization 头
    //        // 注意：这可能包含敏感信息，确保只在安全的环境中这样做
    //        Console.WriteLine("Authorization Header: " + authorizationHeader);
    //        Console.WriteLine("Token validated successfully.");
    //        return Task.CompletedTask;
    //    }
    //};
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
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint($"v1/swagger.json", "图书馆共建图书目录测试");
});

app.UseStaticFiles();
app.UseAuthentication();
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