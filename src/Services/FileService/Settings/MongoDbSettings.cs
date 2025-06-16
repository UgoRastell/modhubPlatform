namespace FileService.Settings
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string FileMetadataCollection { get; set; } = "filemetadata";
        public string ScanResultsCollection { get; set; } = "scanresults";
    }
}
