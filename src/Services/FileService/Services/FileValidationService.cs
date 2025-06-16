using FileService.Models;
using FileService.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Service for validating file metadata and content
    /// </summary>
    public class FileValidationService : IFileValidationService
    {
        private readonly FileSettings _fileSettings;
        private readonly ILogger<FileValidationService> _logger;
        
        // Common MIME types for validation
        private static readonly Dictionary<string, List<string>> _allowedMimeTypesByCategory = new()
        {
            ["image"] = new() { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp", "image/svg+xml" },
            ["document"] = new() { "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "text/plain" },
            ["archive"] = new() { "application/zip", "application/x-rar-compressed", "application/x-7z-compressed", "application/gzip" },
            ["audio"] = new() { "audio/mpeg", "audio/ogg", "audio/wav", "audio/webm" },
            ["video"] = new() { "video/mp4", "video/webm", "video/ogg" },
            ["executable"] = new() { "application/x-msdownload", "application/x-executable" }
        };
        
        // Signatures for common file types (magic numbers)
        private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new()
        {
            ["image/jpeg"] = new() { new byte[] { 0xFF, 0xD8, 0xFF } },
            ["image/png"] = new() { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            ["image/gif"] = new() { new byte[] { 0x47, 0x49, 0x46, 0x38 } },
            ["application/pdf"] = new() { new byte[] { 0x25, 0x50, 0x44, 0x46 } }, // %PDF
            ["application/zip"] = new() { new byte[] { 0x50, 0x4B, 0x03, 0x04 } },  // PK..
            ["application/x-rar-compressed"] = new() { new byte[] { 0x52, 0x61, 0x72, 0x21 } }, // Rar!
            ["audio/mpeg"] = new() { new byte[] { 0x49, 0x44, 0x33 } }, // ID3 (MP3)
            ["video/mp4"] = new() { new byte[] { 0x66, 0x74, 0x79, 0x70 } }, // ftyp (offset 4)
            ["application/x-msdownload"] = new() { new byte[] { 0x4D, 0x5A } }  // MZ (EXE)
        };

        public FileValidationService(
            FileSettings fileSettings,
            ILogger<FileValidationService> logger)
        {
            _fileSettings = fileSettings;
            _logger = logger;
        }

        /// <summary>
        /// Validates file metadata against configured rules
        /// </summary>
        public Task<ValidationResult> ValidateFileMetadataAsync(FileMetadata metadata, FileValidationOptions options = null)
        {
            options ??= new FileValidationOptions();
            
            _logger.LogInformation("Validating file metadata for {fileName}", metadata.FileName);
            
            var result = new ValidationResult
            {
                IsValid = true,
                Messages = new List<string>()
            };
            
            // Check if file type is allowed
            if (options.ValidateFileType && !IsAllowedFileType(metadata.ContentType, options.AllowedFileCategories))
            {
                result.IsValid = false;
                result.Messages.Add($"File type {metadata.ContentType} is not allowed");
                _logger.LogWarning("Invalid file type: {contentType}", metadata.ContentType);
            }
            
            // Check file size
            if (options.ValidateFileSize && metadata.FileSizeBytes > _fileSettings.MaxFileSizeBytes)
            {
                result.IsValid = false;
                result.Messages.Add($"File size exceeds the maximum allowed size of {_fileSettings.MaxFileSizeBytes / (1024 * 1024)} MB");
                _logger.LogWarning("File size {size} bytes exceeds limit of {limit} bytes", 
                    metadata.FileSizeBytes, _fileSettings.MaxFileSizeBytes);
            }
            
            // Check file name
            if (options.ValidateFileName)
            {
                if (string.IsNullOrEmpty(metadata.FileName))
                {
                    result.IsValid = false;
                    result.Messages.Add("File name cannot be empty");
                    _logger.LogWarning("Empty file name detected");
                }
                else
                {
                    // Check for invalid characters in filename
                    var invalidCharsRegex = new Regex("[\\\\/:*?\"<>|]");
                    if (invalidCharsRegex.IsMatch(metadata.FileName))
                    {
                        result.IsValid = false;
                        result.Messages.Add("File name contains invalid characters");
                        _logger.LogWarning("File name contains invalid characters: {fileName}", metadata.FileName);
                    }
                    
                    // Check filename length
                    if (metadata.FileName.Length > _fileSettings.MaxFileNameLength)
                    {
                        result.IsValid = false;
                        result.Messages.Add($"File name exceeds maximum length of {_fileSettings.MaxFileNameLength} characters");
                        _logger.LogWarning("File name too long: {length} characters", metadata.FileName.Length);
                    }
                }
            }
            
            // Check for required metadata
            if (options.RequireEntityAssociation && 
                string.IsNullOrEmpty(metadata.EntityId) || string.IsNullOrEmpty(metadata.EntityType))
            {
                result.IsValid = false;
                result.Messages.Add("File must be associated with an entity (mod, profile, etc.)");
                _logger.LogWarning("Missing entity association for file {fileName}", metadata.FileName);
            }
            
            _logger.LogInformation("File metadata validation completed for {fileName}: {isValid}", 
                metadata.FileName, result.IsValid);
            
            return Task.FromResult(result);
        }

        /// <summary>
        /// Validates file content against configured rules
        /// </summary>
        public async Task<ValidationResult> ValidateFileContentAsync(Stream fileContent, string fileName, 
            string contentType, FileValidationOptions options = null)
        {
            options ??= new FileValidationOptions();
            
            _logger.LogInformation("Validating file content for {fileName}", fileName);
            
            var result = new ValidationResult
            {
                IsValid = true,
                Messages = new List<string>()
            };
            
            // Reset stream position
            if (fileContent.CanSeek)
            {
                fileContent.Position = 0;
            }
            
            // Check file size
            if (options.ValidateFileSize && fileContent.Length > _fileSettings.MaxFileSizeBytes)
            {
                result.IsValid = false;
                result.Messages.Add($"File size exceeds the maximum allowed size of {_fileSettings.MaxFileSizeBytes / (1024 * 1024)} MB");
                _logger.LogWarning("File size {size} bytes exceeds limit of {limit} bytes",
                    fileContent.Length, _fileSettings.MaxFileSizeBytes);
            }
            
            // Verify content type by checking file signature (magic numbers)
            if (options.ValidateContentType && !await VerifyFileSignatureAsync(fileContent, contentType))
            {
                result.IsValid = false;
                result.Messages.Add("File content does not match the declared content type");
                _logger.LogWarning("File signature does not match content type {contentType} for {fileName}", 
                    contentType, fileName);
            }
            
            // Verify that executables are only allowed when explicitly permitted
            if (contentType.Contains("executable") && !options.AllowExecutables)
            {
                result.IsValid = false;
                result.Messages.Add("Executable files are not permitted");
                _logger.LogWarning("Attempted to upload executable file {fileName}", fileName);
            }
            
            // Reset stream position for downstream use
            if (fileContent.CanSeek)
            {
                fileContent.Position = 0;
            }
            
            _logger.LogInformation("File content validation completed for {fileName}: {isValid}", 
                fileName, result.IsValid);
            
            return result;
        }

        /// <summary>
        /// Verifies if a file content type is allowed based on configured categories
        /// </summary>
        private bool IsAllowedFileType(string contentType, IEnumerable<string> allowedCategories)
        {
            // If no categories specified, use default from settings
            if (allowedCategories == null || !allowedCategories.Any())
            {
                allowedCategories = _fileSettings.DefaultAllowedFileCategories;
            }
            
            // If still no categories, allow all
            if (allowedCategories == null || !allowedCategories.Any())
            {
                return true;
            }
            
            // Check if content type is directly in an allowed category
            foreach (var category in allowedCategories)
            {
                if (_allowedMimeTypesByCategory.TryGetValue(category.ToLower(), out var mimeTypes))
                {
                    if (mimeTypes.Contains(contentType.ToLower()))
                    {
                        return true;
                    }
                }
                
                // Check for wildcard matches (e.g., "image/*")
                if (category.EndsWith("/*"))
                {
                    var baseCategory = category.Substring(0, category.Length - 2);
                    if (contentType.StartsWith(baseCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Verifies file content against its declared content type using file signatures
        /// </summary>
        private async Task<bool> VerifyFileSignatureAsync(Stream fileContent, string contentType)
        {
            // Reset stream position
            if (fileContent.CanSeek)
            {
                fileContent.Position = 0;
            }
            else
            {
                _logger.LogWarning("Cannot verify file signature for non-seekable stream");
                return true; // Can't verify, assume it's valid
            }
            
            // If we don't have signatures for this content type, return true
            if (!_fileSignatures.TryGetValue(contentType.ToLower(), out var signatures))
            {
                return true;
            }
            
            // Read signature bytes from file
            var maxSignatureLength = signatures.Max(s => s.Length);
            var buffer = new byte[Math.Min(maxSignatureLength, 50)]; // Read up to 50 bytes
            
            var bytesRead = await fileContent.ReadAsync(buffer, 0, buffer.Length);
            
            if (bytesRead < 4) // Minimum reasonable signature length
            {
                _logger.LogWarning("File is too small to verify signature");
                return false;
            }
            
            // Special case for MP4 files where the signature is at offset 4
            if (contentType.Equals("video/mp4", StringComparison.OrdinalIgnoreCase) && bytesRead >= 8)
            {
                var mp4Sig = new byte[4];
                Array.Copy(buffer, 4, mp4Sig, 0, 4);
                if (CompareSignature(mp4Sig, _fileSignatures["video/mp4"][0]))
                {
                    return true;
                }
            }
            
            // Check against all known signatures for this type
            foreach (var signature in signatures)
            {
                if (CompareSignature(buffer, signature))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Compares file bytes to a signature
        /// </summary>
        private bool CompareSignature(byte[] fileBytes, byte[] signature)
        {
            if (fileBytes.Length < signature.Length)
            {
                return false;
            }
            
            for (int i = 0; i < signature.Length; i++)
            {
                if (fileBytes[i] != signature[i])
                {
                    return false;
                }
            }
            
            return true;
        }
    }

    /// <summary>
    /// Options for file validation
    /// </summary>
    public class FileValidationOptions
    {
        public bool ValidateFileType { get; set; } = true;
        public bool ValidateFileSize { get; set; } = true;
        public bool ValidateFileName { get; set; } = true;
        public bool ValidateContentType { get; set; } = true;
        public bool RequireEntityAssociation { get; set; } = true;
        public bool AllowExecutables { get; set; } = false;
        public IEnumerable<string> AllowedFileCategories { get; set; } = new List<string>();
    }

    /// <summary>
    /// Result of file validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Messages { get; set; }
    }
}
