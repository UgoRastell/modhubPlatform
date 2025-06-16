using FileService.Models;
using FileService.Settings;
using FileService.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Core service for file operations in the File Service microservice
    /// </summary>
    public class FileService : IFileService
    {
        private readonly IFileMetadataRepository _metadataRepository;
        private readonly IScanResultRepository _scanResultRepository;
        private readonly IStorageService _storageService;
        private readonly IVirusScanner _virusScanner;
        private readonly IFileValidationService _validationService;
        private readonly IFileProcessingQueue _processingQueue;
        private readonly IRabbitMQService _eventBus;
        private readonly FileSettings _fileSettings;
        private readonly ILogger<FileService> _logger;

        public FileService(
            IFileMetadataRepository metadataRepository,
            IScanResultRepository scanResultRepository,
            IStorageService storageService,
            IVirusScanner virusScanner,
            IFileValidationService validationService,
            IFileProcessingQueue processingQueue,
            IRabbitMQService eventBus,
            FileSettings fileSettings,
            ILogger<FileService> logger)
        {
            _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
            _scanResultRepository = scanResultRepository ?? throw new ArgumentNullException(nameof(scanResultRepository));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _virusScanner = virusScanner ?? throw new ArgumentNullException(nameof(virusScanner));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _processingQueue = processingQueue ?? throw new ArgumentNullException(nameof(processingQueue));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _fileSettings = fileSettings ?? throw new ArgumentNullException(nameof(fileSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Uploads a file and initiates processing
        /// </summary>
        public async Task<FileMetadata> UploadFileAsync(Stream fileStream, string fileName, string contentType, string userId, 
            string entityId = null, string entityType = null, bool isPublic = false, Dictionary<string, string> metadata = null)
        {
            try
            {
                _logger.LogInformation("Beginning file upload process for {fileName}, size: {size} bytes", 
                    fileName, fileStream.Length);
                
                // Create file metadata
                var fileMetadata = new FileMetadata
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = fileName,
                    ContentType = contentType,
                    FileSizeBytes = fileStream.Length,
                    UploadDate = DateTime.UtcNow,
                    UserId = userId,
                    EntityId = entityId,
                    EntityType = entityType,
                    IsPublic = isPublic,
                    Status = FileStatus.Pending,
                    CustomMetadata = metadata ?? new Dictionary<string, string>()
                };
                
                // Validate file metadata
                var metadataValidationResult = await _validationService.ValidateFileMetadataAsync(fileMetadata);
                if (!metadataValidationResult.IsValid)
                {
                    _logger.LogWarning("File metadata validation failed for {fileName}: {reasons}",
                        fileName, string.Join("; ", metadataValidationResult.Messages));
                    
                    throw new ValidationException(string.Join("; ", metadataValidationResult.Messages));
                }
                
                // Validate file content
                var contentValidationResult = await _validationService.ValidateFileContentAsync(
                    fileStream, fileName, contentType);
                    
                if (!contentValidationResult.IsValid)
                {
                    _logger.LogWarning("File content validation failed for {fileName}: {reasons}",
                        fileName, string.Join("; ", contentValidationResult.Messages));
                    
                    throw new ValidationException(string.Join("; ", contentValidationResult.Messages));
                }
                
                // Reset stream position after validation
                if (fileStream.CanSeek)
                {
                    fileStream.Position = 0;
                }
                
                // Upload to storage (private container by default)
                var containerName = isPublic ? _fileSettings.PublicContainerName : _fileSettings.PrivateContainerName;
                fileMetadata.StorageLocation = await _storageService.UploadFileAsync(fileStream, fileName, contentType, containerName);
                
                // Set storage-related metadata
                fileMetadata.StorageContainer = containerName;
                fileMetadata.LastModified = DateTime.UtcNow;
                
                // Save metadata to database
                await _metadataRepository.CreateAsync(fileMetadata);
                
                _logger.LogInformation("File {fileName} uploaded successfully with ID {fileId}",
                    fileName, fileMetadata.Id);
                
                // Queue for virus scanning if enabled
                if (_fileSettings.EnableVirusScan)
                {
                    _logger.LogInformation("Queueing file {fileId} for virus scanning", fileMetadata.Id);
                    await _processingQueue.EnqueueAsync(new FileProcessingItem
                    {
                        FileId = fileMetadata.Id,
                        Operation = FileOperation.ScanForViruses,
                        Priority = FileProcessingPriority.High
                    });
                }
                else
                {
                    // If scanning is disabled, mark as verified
                    fileMetadata.Status = FileStatus.Available;
                    await _metadataRepository.UpdateAsync(fileMetadata);
                }
                
                // For image files, generate thumbnail if enabled
                if (_fileSettings.AutoGenerateThumbnails && 
                    contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Queueing thumbnail generation for image {fileId}", fileMetadata.Id);
                    await _processingQueue.EnqueueAsync(new FileProcessingItem
                    {
                        FileId = fileMetadata.Id,
                        Operation = FileOperation.GenerateThumbnail,
                        Priority = FileProcessingPriority.Medium
                    });
                }
                
                // Publish file uploaded event
                await _eventBus.PublishAsync(new FileUploadedEvent
                {
                    FileId = fileMetadata.Id,
                    FileName = fileMetadata.FileName,
                    ContentType = fileMetadata.ContentType,
                    UserId = fileMetadata.UserId,
                    EntityId = fileMetadata.EntityId,
                    EntityType = fileMetadata.EntityType,
                    IsPublic = fileMetadata.IsPublic,
                    FileSizeBytes = fileMetadata.FileSizeBytes,
                    Timestamp = DateTime.UtcNow
                });
                
                return fileMetadata;
            }
            catch (Exception ex) when (ex is not ValidationException)
            {
                _logger.LogError(ex, "Error uploading file {fileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Downloads a file
        /// </summary>
        public async Task<(Stream FileStream, FileMetadata Metadata)> DownloadFileAsync(string fileId, string userId)
        {
            try
            {
                _logger.LogInformation("Attempting to download file {fileId} for user {userId}", fileId, userId);
                
                // Retrieve file metadata
                var metadata = await _metadataRepository.GetByIdAsync(fileId);
                if (metadata == null)
                {
                    _logger.LogWarning("File {fileId} not found", fileId);
                    throw new FileNotFoundException($"File with id {fileId} not found");
                }
                
                // Check access permissions
                if (!metadata.IsPublic && metadata.UserId != userId)
                {
                    _logger.LogWarning("User {userId} attempted to access file {fileId} without permission", 
                        userId, fileId);
                    throw new UnauthorizedAccessException("You do not have permission to access this file");
                }
                
                // Check file status
                if (metadata.Status != FileStatus.Available)
                {
                    _logger.LogWarning("Attempted to download unavailable file {fileId} with status {status}", 
                        fileId, metadata.Status);
                    throw new InvalidOperationException($"File is not available for download (status: {metadata.Status})");
                }
                
                // Download from storage
                var fileStream = await _storageService.DownloadFileAsync(
                    metadata.StorageLocation, metadata.StorageContainer);
                
                if (fileStream == null)
                {
                    _logger.LogError("File {fileId} not found in storage at {location}", 
                        fileId, metadata.StorageLocation);
                    throw new FileNotFoundException($"File content not found in storage");
                }
                
                _logger.LogInformation("File {fileId} downloaded successfully", fileId);
                
                // Record download if tracking enabled
                if (_fileSettings.TrackDownloads)
                {
                    // Increment download count
                    metadata.DownloadCount++;
                    metadata.LastAccessed = DateTime.UtcNow;
                    await _metadataRepository.UpdateAsync(metadata);
                }
                
                return (fileStream, metadata);
            }
            catch (Exception ex) when (ex is not FileNotFoundException && ex is not UnauthorizedAccessException && ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Error downloading file {fileId}", fileId);
                throw;
            }
        }

        /// <summary>
        /// Deletes a file and its associated resources
        /// </summary>
        public async Task<bool> DeleteFileAsync(string fileId, string userId, bool forceDelete = false)
        {
            try
            {
                _logger.LogInformation("Attempting to delete file {fileId} by user {userId}", fileId, userId);
                
                // Retrieve file metadata
                var metadata = await _metadataRepository.GetByIdAsync(fileId);
                if (metadata == null)
                {
                    _logger.LogWarning("File {fileId} not found for deletion", fileId);
                    return false;
                }
                
                // Check access permissions unless force delete is specified
                if (!forceDelete && metadata.UserId != userId)
                {
                    _logger.LogWarning("User {userId} attempted to delete file {fileId} without permission", 
                        userId, fileId);
                    throw new UnauthorizedAccessException("You do not have permission to delete this file");
                }
                
                // Delete from storage
                bool storageDeleted = await _storageService.DeleteFileAsync(
                    metadata.StorageLocation, metadata.StorageContainer);
                
                if (!storageDeleted)
                {
                    _logger.LogWarning("Failed to delete file {fileId} from storage at {location}", 
                        fileId, metadata.StorageLocation);
                }
                
                // Delete thumbnail if it exists
                if (metadata.HasThumbnail && !string.IsNullOrEmpty(metadata.ThumbnailLocation))
                {
                    bool thumbnailDeleted = await _storageService.DeleteFileAsync(
                        metadata.ThumbnailLocation, _fileSettings.ThumbnailContainerName);
                    
                    if (!thumbnailDeleted)
                    {
                        _logger.LogWarning("Failed to delete thumbnail for file {fileId}", fileId);
                    }
                }
                
                // Delete scan results
                await _scanResultRepository.DeleteByFileIdAsync(fileId);
                
                // Delete metadata from database
                bool metadataDeleted = await _metadataRepository.DeleteAsync(fileId);
                
                if (!metadataDeleted)
                {
                    _logger.LogWarning("Failed to delete metadata for file {fileId}", fileId);
                }
                
                _logger.LogInformation("File {fileId} deleted successfully", fileId);
                
                // Publish file deleted event
                await _eventBus.PublishAsync(new FileDeletedEvent
                {
                    FileId = fileId,
                    UserId = metadata.UserId,
                    EntityId = metadata.EntityId,
                    EntityType = metadata.EntityType,
                    FileName = metadata.FileName,
                    Timestamp = DateTime.UtcNow
                });
                
                return storageDeleted && metadataDeleted;
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Error deleting file {fileId}", fileId);
                throw;
            }
        }

        /// <summary>
        /// Gets file metadata by ID
        /// </summary>
        public async Task<FileMetadata> GetFileMetadataAsync(string fileId, string userId)
        {
            try
            {
                _logger.LogInformation("Getting metadata for file {fileId}", fileId);
                
                var metadata = await _metadataRepository.GetByIdAsync(fileId);
                
                if (metadata == null)
                {
                    _logger.LogWarning("File metadata not found for id {fileId}", fileId);
                    return null;
                }
                
                // Check access permissions for non-public files
                if (!metadata.IsPublic && metadata.UserId != userId)
                {
                    _logger.LogWarning("User {userId} attempted to access metadata for file {fileId} without permission", 
                        userId, fileId);
                    throw new UnauthorizedAccessException("You do not have permission to access this file's metadata");
                }
                
                return metadata;
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Error retrieving file metadata for {fileId}", fileId);
                throw;
            }
        }
    }
    
    /// <summary>
    /// Exception thrown when file validation fails
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception inner) : base(message, inner) { }
    }
}
