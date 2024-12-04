using Microsoft.OpenApi.Models;
using SolidarityBookCatalog.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ͼ��ݹ���ͼ��Ŀ¼����", Version = "v1" });
});
//�Զ�����񣬱���ע��
builder.Services.AddSingleton<BookService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<HoldingService>();

var app = builder.Build();
// Configure the HTTP request pipeline.
//��������open api
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint($"v1/swagger.json", "ͼ��ݹ���ͼ��Ŀ¼����");
    });


app.UseAuthorization();

app.MapControllers();

app.Run();
