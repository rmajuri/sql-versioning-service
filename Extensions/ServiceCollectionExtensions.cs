using SqlVersioningService.Services;
using SqlVersioningService.Repositories;
using SqlVersioningService.Infrastructure;

namespace SqlVersioningService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<DatabaseContext>();
        services.AddScoped<QueryRepository>();
        services.AddScoped<VersionRepository>();
        services.AddScoped<QueryVersioningService>();
        services.AddSingleton<HashingService>();
        // Azure Blob storage service registration. Reads configuration from
        // AzureStorage:ConnectionString and AzureStorage:ContainerName. Falls
        // back to AZURE_STORAGE_CONNECTION_STRING env var and default container.
        services.AddSingleton<IBlobStorageService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var conn = config.GetValue<string>("AzureStorage:ConnectionString")
                       ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
                       ?? string.Empty;
            var container = config.GetValue<string>("AzureStorage:ContainerName") ?? "queries";

            if (string.IsNullOrEmpty(conn))
                throw new InvalidOperationException("Azure storage connection string is not configured. Set AzureStorage:ConnectionString or AZURE_STORAGE_CONNECTION_STRING.");

            return new AzureBlobStorageService(conn, container);
        });
        return services;
    }
}
