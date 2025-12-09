using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace SqlVersioningService.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(string connectionString, string containerName)
    {
        // Create container client
        _container = new BlobContainerClient(connectionString, containerName);

        // Ensure container exists (safe to call)
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    private static string GetBlobPath(string hash)
    {
        // Optional: put blobs in a folder for organization
        return $"queries/{hash}";
    }

    public async Task UploadAsync(string hash, string content)
    {
        var blobPath = GetBlobPath(hash);
        var blobClient = _container.GetBlobClient(blobPath);

        // If blob exists, do nothing – deduplication happens upstream
        if (await blobClient.ExistsAsync())
            return;

        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);

        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain; charset=utf-8" },
            }
        );
    }

    public async Task<string> DownloadAsync(string hash)
    {
        var blobPath = GetBlobPath(hash);
        var blobClient = _container.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync())
            throw new FileNotFoundException(
                $"Blob with hash {hash} not found in Azure Blob Storage."
            );

        var response = await blobClient.DownloadContentAsync();
        return response.Value.Content.ToString();
    }
}

