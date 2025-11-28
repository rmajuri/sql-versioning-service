namespace SqlVersioningService.Models;

public class Query
{
    public int Id { get; set; }
    public string Sql { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
