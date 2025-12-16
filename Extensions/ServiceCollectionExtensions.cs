using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Services;
using SqlVersioningService.Repositories;

namespace SqlVersioningService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<DatabaseContext>();

        services.Configure<AzureStorageOptions>(
            config.GetSection("AzureStorage"));

        services.AddSingleton<IBlobStorageService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.ConnectionString))
                throw new InvalidOperationException(
                    "AzureStorage:ConnectionString is not configured.");

            return new AzureBlobStorageService(
                options.ConnectionString,
                options.ContainerName);
        });

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<QueryRepository>();
        services.AddScoped<VersionRepository>();
        services.AddScoped<QueryVersioningService>();
        services.AddScoped<QueryCreationService>();
        services.AddSingleton<HashingService>();

        return services;
    }
}
