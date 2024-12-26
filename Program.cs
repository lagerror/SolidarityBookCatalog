using Microsoft.OpenApi.Models;
using SolidarityBookCatalog.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "图书馆共建图书目录测试", Version = "v1" });
});
//自定义服务，便于注入
builder.Services.AddHttpClient();
builder.Services.AddSingleton<BibliosService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<HoldingService>();
builder.Services.AddSingleton<ElasticService>();

var app = builder.Build();
// Configure the HTTP request pipeline.
//发布后保留open api
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint($"v1/swagger.json", "图书馆共建图书目录测试");
    });

app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();

app.Run();
