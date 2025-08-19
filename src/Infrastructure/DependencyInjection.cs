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

        // Redis configuration - optional, controlled by IsEnabled setting
        var redisEnabledString = configuration["Redis:IsEnabled"];
        var redisEnabled = string.IsNullOrEmpty(redisEnabledString) ? true : bool.Parse(redisEnabledString);
        
        if (redisEnabled)
        {
            var redisConnectionString = configuration["Redis:ConnectionString"];
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                var redisPassword = configuration["Redis:Password"];
                var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
                
                if (!string.IsNullOrEmpty(redisPassword))
                {
                    redisConfig.Password = redisPassword;
                }
                
                var timeoutString = configuration["Redis:ConnectTimeoutMs"];
                var timeout = string.IsNullOrEmpty(timeoutString) ? 5000 : int.Parse(timeoutString);
                redisConfig.ConnectTimeout = timeout;
                redisConfig.AbortOnConnectFail = false; // Allow app to start even if Redis is down
                
                services.AddSingleton<IConnectionMultiplexer>(
                    ConnectionMultiplexer.Connect(redisConfig));
            }
        }
            
        // Repositories and Services
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUrlMappingRepository, UrlMappingRepository>();
        services.AddScoped<IShortUrlGeneratorService, ShortUrlGeneratorService>();


        return services;
    }
}

