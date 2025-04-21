using Business.Services;
using Data;
using Data.Repositories;
using Microsoft.EntityFrameworkCore;
using ZTP1.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddEndpointsApiExplorer();
services.AddControllers();
services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

services.AddScoped<INotificationRepository, NotificationRepository>();
services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
