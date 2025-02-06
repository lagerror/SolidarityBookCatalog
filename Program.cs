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

    //options.Events = new JwtBearerEvents   //��Ϊ���ļ����ԣ�ɾ��Microsoft.IdentityModel.Tokens������
    //{
    //    OnAuthenticationFailed = context =>
    //    {
    //        // ��ȡ�����е� Authorization ͷ
    //        var authorizationHeader = context.Request.Headers["Authorization"];

    //        // ��ӡ���¼���� Authorization ͷ
    //        // ע�⣺����ܰ���������Ϣ��ȷ��ֻ�ڰ�ȫ�Ļ�����������
    //        Console.WriteLine("Authorization Header: " + authorizationHeader);

    //        Console.WriteLine("Authentication failed: " + context.Exception.Message);
    //        return Task.CompletedTask;
    //    },
    //    OnTokenValidated = context =>
    //    {
    //        // ��ȡ�����е� Authorization ͷ
    //        var authorizationHeader = context.Request.Headers["Authorization"];

    //        // ��ӡ���¼���� Authorization ͷ
    //        // ע�⣺����ܰ���������Ϣ��ȷ��ֻ�ڰ�ȫ�Ļ�����������
    //        Console.WriteLine("Authorization Header: " + authorizationHeader);
    //        Console.WriteLine("Token validated successfully.");
    //        return Task.CompletedTask;
    //    }
    //};
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
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint($"v1/swagger.json", "ͼ��ݹ���ͼ��Ŀ¼����");
});

app.UseStaticFiles();
app.UseAuthentication();
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