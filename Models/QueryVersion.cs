namespace SqlVersioningService.Models;

public class QueryVersion
{
    public int Id { get; set; }
    public int QueryId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
