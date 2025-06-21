using System;

namespace Frontend.Models
{
    public class ModFileDto
    {
        public string? Id { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public long SizeInBytes { get; set; } // Alias de FileSize pour la compatibilité existante
        public string? Hash { get; set; }
        public string? DownloadUrl { get; set; }
        public DateTime UploadDate { get; set; }
        public DateTime UploadedAt { get; set; } // Alias de UploadDate pour la compatibilité existante
        public string? Status { get; set; }
        public bool IsPrimary { get; set; }
        public string? FileType { get; set; } // zip, rar, 7z, etc.
        public string? SecurityScanStatus { get; set; }
        public DateTime? SecurityScanDate { get; set; }
    }
}
