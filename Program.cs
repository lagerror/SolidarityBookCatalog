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

// ����Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
   
    .CreateLogger();

builder.Host.UseSerilog(); // ʹ��Serilog��Ϊ��־�ṩ��

// ��������֤�������� JWT Bearer ��֤
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
        IssuerSigningKey = new RsaSecurityKey(GetRsaPublicKey()) // ʹ�ù�Կ��֤
    };
});

// �����Ȩ����
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrManager", policy =>
        policy.RequireRole("admin", "manager"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ͼ��ݹ���ͼ��Ŀ¼����", Version = "v1" });
});

// �Զ������
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ElasticService>();
builder.Services.AddSingleton<ElasticNestService>();
builder.Services.AddSingleton<BibliosService>();
builder.Services.AddSingleton<HoldingService>();
builder.Services.AddSingleton<UserService>();

// ȫ�ֹ��� MongoDB ����
var client = new MongoClient(builder.Configuration.GetValue<string>("ConnectionStrings:BookDb"));
builder.Services.AddSingleton<IMongoClient>(client);

var app = builder.Build();

// ���� HTTP ����ܵ�
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint($"v1/swagger.json", "ͼ��ݹ���ͼ��Ŀ¼����");
    });
}

app.UseStaticFiles();

//ע����������˳��
app.UseAuthentication();
// �����־������Enricher
app.Use(async (context, next) =>
{
    LogContext.PushProperty("IP", context.Connection.RemoteIpAddress?.ToString());
    LogContext.PushProperty("User", context.User.Identity.Name ?? "Anonymous");
    await next();
});
// ʹ�� Serilog �滻Ĭ�ϵ���־ϵͳ
app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();

app.Run();

 RSA GetRsaPublicKey()
{
    //�����ͷ�rsa����ʹ��static����ע�������ļ���ȡ
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

        rsa.ImportFromPem(publicKeyPem); // ���� PEM ��ʽ�Ĺ�Կ
        return rsa;
    
}