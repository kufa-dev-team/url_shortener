using Application;
using Application.Services;
using Domain.Interfaces;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IUrlMappingService, UrlMappingService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("ConnectionStrings") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(configuration);
});


// Add application layers
builder.Services.AddApplication();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The connection string 'DefaultConnection' was not found or is empty. Please check your configuration.");

builder.Services.AddInfrastructure(connectionString, builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapScalarApiReference();

}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();