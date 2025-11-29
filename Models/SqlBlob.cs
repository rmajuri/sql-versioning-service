namespace SqlVersioningService.Models;

public class SqlBlob
{
    public string Hash { get; set; } = string.Empty;

    public string BytesLocation { get; set; } = string.Empty;

    public int BytesSize { get; set; }
}
