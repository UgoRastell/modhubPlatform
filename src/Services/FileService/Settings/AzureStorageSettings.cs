namespace FileService.Settings
{
    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; }
        public string ModFilesContainerName { get; set; } = "mod-files";
        public string ImagesContainerName { get; set; } = "images";
        public string QuarantineContainerName { get; set; } = "quarantine";
        public string TempContainerName { get; set; } = "temp-uploads";
        public int SasTokenExpiryHours { get; set; } = 2;
        public bool UseHttps { get; set; } = true;
    }
}
