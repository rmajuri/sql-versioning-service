using SqlVersioningService.Repositories;
using SqlVersioningService.Services;

namespace SqlVersioningService.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHashingService _hashingService;

    // Paths that bypass authentication
    private static readonly string[] ExcludedPaths = { "/health", "/swagger", "/favicon.ico" };

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, IHashingService hashingService)
    {
        _next = next;
        _hashingService = hashingService;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyRepository apiKeyRepository)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Allow health checks and swagger without authentication
        if (IsExcludedPath(path))
        {
            await _next(context);
            return;
        }

        // Check for Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing Authorization header");
            return;
        }

        var headerValue = authHeader.ToString();

        // Validate Bearer token format
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync(
                "Invalid Authorization header format. Expected: Bearer <api_key>"
            );
            return;
        }

        var apiKey = headerValue.Substring("Bearer ".Length).Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key is empty");
            return;
        }

        // Hash the provided key and validate against the database
        var hashedKey = _hashingService.ComputeHash(apiKey);
        var isValid = await apiKeyRepository.IsValidHashedKeyAsync(hashedKey);

        if (!isValid)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or revoked API key");
            return;
        }

        await _next(context);
    }

    private static bool IsExcludedPath(string path)
    {
        foreach (var excluded in ExcludedPaths)
        {
            if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Extension method for registering the API key authentication middleware.
/// </summary>
public static class ApiKeyAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
