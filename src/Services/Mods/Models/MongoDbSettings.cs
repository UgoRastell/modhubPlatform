namespace ModsService.Models;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string ModsCollectionName { get; set; } = null!;
}
