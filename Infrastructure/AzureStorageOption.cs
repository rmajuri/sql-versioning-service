namespace SqlVersioningService.Infrastructure;

public sealed class AzureStorageOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string ContainerName { get; init; } = "queries";

    // Optional: full URI to the blob container when using token-based auth (managed identity)
    // Example: https://<account>.blob.core.windows.net/<container>
    public string ContainerUri { get; init; } = string.Empty;
}
