using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ModsService.Models
{
    public class ModUploadDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string GameId { get; set; }
        public string Version { get; set; }
        public IFormFile ModFile { get; set; }
        public IFormFile ThumbnailFile { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
