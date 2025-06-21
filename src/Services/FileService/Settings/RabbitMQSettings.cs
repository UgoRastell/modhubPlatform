namespace FileService.Settings
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        
        // Exchange names
        public string FileExchange { get; set; } = "modhub.file";
        public string ModExchange { get; set; } = "modhub.mod";
        
        // Queue names
        public string FileQueue { get; set; } = "file-service";
        
        // Routing keys
        public string FileUploadedRoutingKey { get; set; } = "file.uploaded";
        public string FileScannedRoutingKey { get; set; } = "file.scanned";
        public string FileProcessedRoutingKey { get; set; } = "file.processed";
        public string FileDeletedRoutingKey { get; set; } = "file.deleted";
    }
}
