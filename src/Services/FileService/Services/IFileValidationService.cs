using FileService.Models;

namespace FileService.Services
{
    public interface IFileValidationService
    {
        Task<ValidationResult> ValidateFileAsync(Stream fileStream, string fileName, FileType fileType);
        bool IsAllowedExtension(string extension, FileType fileType);
        bool IsAllowedContentType(string contentType, FileType fileType);
        bool IsFileSizeAllowed(long fileSize, FileType fileType);
        Task<bool> ValidateImageDimensionsAsync(Stream imageStream, int maxWidth, int maxHeight);
        Task<string> CalculateFileHashAsync(Stream fileStream);
        Task<Dictionary<string, string>> ExtractMetadataAsync(Stream fileStream, string fileName, string contentType);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public string FileHash { get; set; }
    }
}
