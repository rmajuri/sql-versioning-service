using System;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlVersioningService.Infrastructure;
using SqlVersioningService.Repositories;
using SqlVersioningService.Services;

namespace SqlVersioningService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        services.AddSingleton<DatabaseContext>();

        services.Configure<AzureStorageOptions>(config.GetSection("AzureStorage"));

        services.AddSingleton<IBlobStorageService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureStorageOptions>>().Value;

            // If a connection string is provided, use it (supports Azurite and account keys)
            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                var containerClient = new BlobContainerClient(
                    options.ConnectionString,
                    options.ContainerName
                );
                return new AzureBlobStorageService(containerClient);
            }

            // If a container URI is provided, assume token-based auth (e.g. managed identity)
            if (!string.IsNullOrWhiteSpace(options.ContainerUri))
            {
                var credential = new DefaultAzureCredential();
                var containerClient = new BlobContainerClient(
                    new Uri(options.ContainerUri),
                    credential
                );
                return new AzureBlobStorageService(containerClient);
            }

            throw new InvalidOperationException(
                "AzureStorage:ConnectionString or AzureStorage:ContainerUri must be configured."
            );
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
