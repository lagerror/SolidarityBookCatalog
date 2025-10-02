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

// ����Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
   
    .CreateLogger();

builder.Host.UseSerilog(); // ʹ��Serilog��Ϊ��־�ṩ��
//
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
});
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
    options.AddPolicy("ManagerOrReader", policy =>
        policy.RequireRole("admin, manager","reader"));
});

builder.Services.AddControllers();
// ����CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", builder =>
    {
        builder.AllowAnyOrigin()
                //.WithOrigins("http://localhost:5173") // ���������
               .AllowAnyHeader()                 // �����κ�ͷ
               .AllowAnyMethod();                // �����κη���
    });
});

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
builder.Services.AddSingleton<ReaderService>();
builder.Services.AddSingleton<LoanWorkService>();
builder.Services.AddSingleton<IsdlLoanService>();
builder.Services.AddSingleton<ToolService>();
builder.Services.AddSingleton<FiscoService>();
builder.Services.AddSingleton<MinioService>();


// ����ڴ滺��
builder.Services.AddMemoryCache();
// ע��΢�� Token ���񣨵�����
builder.Services.AddSingleton<IWeChatService, WeChatService>();
//ע��ٶ�IOT����
builder.Services.AddSingleton<IBDIot,BDIot>();

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
// ʹ��CORS�м��
app.UseCors("AllowSpecificOrigin");
//ע����������˳��
app.UseAuthentication();
// �����־������Enricher
app.Use(async (context, next) =>
{
    // ���Դ�X-Forwarded-Forͷ�л�ȡIP��ַ
    var forwardedFor = context.Request.Headers["X-Forwarded-For"];
    string ip = null;
    if (!StringValues.IsNullOrEmpty(forwardedFor))
    {
        // X-Forwarded-Forͷ���ܰ������IP��ַ����һ���ǿͻ��˵���ʵIP
        ip = forwardedFor.ToString().Split(',')[0].Trim();
    }
    else
    {
        // ���X-Forwarded-Forͷ�����ڣ����Դ�X-Real-IPͷ��ȡ
        var realIp = context.Request.Headers["X-Real-IP"];
        if (!StringValues.IsNullOrEmpty(realIp))
        {
            ip = realIp.ToString();
        }
    }

    // ���ͷ�ж�û���ҵ�IP��ַ��ʹ�����ӵ�RemoteIpAddress
    if (string.IsNullOrEmpty(ip) && context.Connection.RemoteIpAddress != null)
    {
        ip = context.Connection.RemoteIpAddress.ToString();
    }

    // ���IP��ַ��Ȼ�Ǳ��ص�ַ�����Խ�һ��������¼
    if (ip == "127.0.0.1" || ip == "::1")
    {
        // ���Ը�����Ҫ�������⴦��
    }

    // ��ӵ���־������
    LogContext.PushProperty("IP", ip ?? "Unknown");
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