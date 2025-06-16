namespace FileService.Settings
{
    public class FileSettings
    {
        // Maximum file sizes in bytes
        public long MaxModFileSize { get; set; } = 1073741824; // 1GB default
        public long MaxImageFileSize { get; set; } = 5242880; // 5MB default
        public long MaxAvatarFileSize { get; set; } = 2097152; // 2MB default
        
        // Allowed file extensions
        public List<string> AllowedModExtensions { get; set; } = new List<string> { ".zip", ".rar", ".7z" };
        public List<string> AllowedImageExtensions { get; set; } = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        
        // Virus scanning settings
        public bool EnableVirusScanning { get; set; } = true;
        public int ScanTimeoutSeconds { get; set; } = 60;
        public bool QuarantineMaliciousFiles { get; set; } = true;
        
        // File processing settings
        public int MaxConcurrentUploads { get; set; } = 10;
        public int ProcessingQueueCapacity { get; set; } = 100;
        public int ProcessingRetryLimit { get; set; } = 3;
        
        // Image processing settings
        public bool ResizeImages { get; set; } = true;
        public int MaxImageWidth { get; set; } = 2048;
        public int MaxImageHeight { get; set; } = 2048;
        public int ThumbnailWidth { get; set; } = 320;
        public int ThumbnailHeight { get; set; } = 180;
        public int AvatarSize { get; set; } = 256;
        
        // Cache control
        public int CacheDurationMinutes { get; set; } = 60;
        public bool EnableCdn { get; set; } = true;
    }
}
