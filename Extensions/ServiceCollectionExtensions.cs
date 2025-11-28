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
        return services;
    }
}
