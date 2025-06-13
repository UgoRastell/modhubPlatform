namespace UsersService.Data;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string UsersCollectionName { get; set; } = null!;
    public string RefreshTokensCollectionName { get; set; } = null!;
    public string UserActivitiesCollectionName { get; set; } = null!;
}
