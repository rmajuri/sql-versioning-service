public interface IBlobStorageService
{
    Task UploadAsync(string hash, string content);
    Task<string> DownloadAsync(string hash);
}
