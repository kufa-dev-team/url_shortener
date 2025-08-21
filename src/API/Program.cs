using Application;
using Application.Services;
using Domain.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using NewRelic.Api.Agent;
using Prometheus;
using Scalar.AspNetCore;
using StackExchange.Redis;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();




var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("The connection string 'DefaultConnection' was not found or is empty. Please check your configuration.");

// Add health checks  
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
var redisPassword = builder.Configuration["Redis:Password"];
if (!string.IsNullOrEmpty(redisPassword))
{
    redisConnectionString += $",password={redisPassword}";
}

builder.Services.AddScoped<IUrlMappingService, UrlMappingService>();

// Add application layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, builder.Configuration);

// Add health checks with Prometheus integration
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database", tags: new[] { "db", "postgresql" })
    .AddRedis(redisConnectionString, name: "redis-cache", tags: new[] { "cache", "redis" })
    .ForwardToPrometheus();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapScalarApiReference();

}

// Add metrics endpoint
app.UseRouting();
app.UseHttpMetrics(); // Add this for HTTP metrics
app.MapMetrics(); // Add this to expose /metrics endpoint

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Only basic liveness check
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db") || check.Tags.Contains("cache")
});

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            entries = report.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new { status = kvp.Value.Status.ToString() }
            )
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();
