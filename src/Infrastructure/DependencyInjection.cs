using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;


namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Redis
        var redisConnectionString = configuration["Redis:ConnectionStrings"];
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            throw new ArgumentNullException("Redis:ConnectionStrings", "Redis connection string is not configured.");
        }
        var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConfig));
            
        // Repositories and Services
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUrlMappingRepository, UrlMappingRepository>();
        services.AddScoped<IShortUrlGeneratorService, ShortUrlGeneratorService>();


        return services;
    }
}

