using System;
using System.Collections.Generic;

namespace Frontend.Models
{
    public class ModVersionDto
    {
        public string? Id { get; set; }
        public string? ModId { get; set; }
        public string? VersionNumber { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Changelog { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? DownloadUrl { get; set; }
        public long DownloadCount { get; set; }
        public string? FileSize { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsLatest { get; set; }
        public string? Status { get; set; }
        public ModFileDto? MainFile { get; set; }
        public CompatibilityInfoDto? Compatibility { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
