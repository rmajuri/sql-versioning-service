namespace SqlVersioningService.Infrastructure;

public sealed class AzureStorageOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string ContainerName { get; init; } = "queries";
}