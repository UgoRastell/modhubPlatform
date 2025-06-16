using FileService.Events;
using FileService.Models;
using FileService.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileService.Services
{
    /// <summary>
    /// Background service for processing file operations asynchronously
    /// </summary>
    public class FileProcessingService : BackgroundService
    {
        private readonly IFileProcessingQueue _processingQueue;
        private readonly IFileService _fileService;
        private readonly IFileMetadataRepository _metadataRepository;
        private readonly IScanResultRepository _scanResultRepository;
        private readonly IStorageService _storageService;
        private readonly IVirusScanner _virusScanner;
        private readonly IRabbitMQService _eventBus;
        private readonly FileSettings _fileSettings;
        private readonly ILogger<FileProcessingService> _logger;
        
        // Use a timer for recurring operations
        private Timer _processingTimer;
        private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);
        private int _maxConcurrentOperations = 5;
        private SemaphoreSlim _throttleSemaphore;
        
        // Track ongoing operations
        private int _activeOperations = 0;
        private bool _isProcessing = false;

        public FileProcessingService(
            IFileProcessingQueue processingQueue,
            IFileService fileService,
            IFileMetadataRepository metadataRepository,
            IScanResultRepository scanResultRepository,
            IStorageService storageService,
            IVirusScanner virusScanner,
            IRabbitMQService eventBus,
            FileSettings fileSettings,
            ILogger<FileProcessingService> logger)
        {
            _processingQueue = processingQueue ?? throw new ArgumentNullException(nameof(processingQueue));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _metadataRepository = metadataRepository ?? throw new ArgumentNullException(nameof(metadataRepository));
            _scanResultRepository = scanResultRepository ?? throw new ArgumentNullException(nameof(scanResultRepository));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _virusScanner = virusScanner ?? throw new ArgumentNullException(nameof(virusScanner));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _fileSettings = fileSettings ?? throw new ArgumentNullException(nameof(fileSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Set max concurrent operations from settings or default
            _maxConcurrentOperations = _fileSettings.MaxConcurrentFileOperations > 0 
                ? _fileSettings.MaxConcurrentFileOperations 
                : 5;
                
            _throttleSemaphore = new SemaphoreSlim(_maxConcurrentOperations);
        }

        /// <summary>
        /// Called when the service is starting
        /// </summary>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("File Processing Service is starting");
            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Main execution method for the background service
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File Processing Service is running");

            // Create a timer that triggers processing at regular intervals
            _processingTimer = new Timer(
                ProcessQueueItems,
                null,
                TimeSpan.Zero,                  // Start immediately
                _processingInterval);           // Then repeat at interval
                
            // Keep the service running until requested to stop
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Timer callback to process queue items
        /// </summary>
        private void ProcessQueueItems(object state)
        {
            // Avoid overlapping executions
            if (_isProcessing)
            {
                return;
            }
            
            _isProcessing = true;
            
            try
            {
                // Process as many items as possible up to concurrency limit
                while (_activeOperations < _maxConcurrentOperations)
                {
                    var processingTask = DequeueAndProcessNextItemAsync();
                    
                    // If no items to process, break the loop
                    if (processingTask == null)
                    {
                        break;
                    }
                }
                
                _logger.LogDebug("File Processing Service cycle completed. Active operations: {count}", _activeOperations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessQueueItems timer callback");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Dequeues next item and starts processing
        /// </summary>
        private Task DequeueAndProcessNextItemAsync()
        {
            // Try to dequeue next item
            var dequeueTask = _processingQueue.DequeueAsync();
            
            // If queue is empty, return completed task
            if (!dequeueTask.Wait(100))
            {
                return null;
            }
            
            var item = dequeueTask.Result;
            if (item == null)
            {
                return null;
            }
            
            _logger.LogInformation("Processing file operation {operation} for file {fileId} with priority {priority}", 
                item.Operation, item.FileId, item.Priority);
            
            // Increment active operations counter
            Interlocked.Increment(ref _activeOperations);
            
            // Start processing task
            Task.Run(async () => 
            {
                try
                {
                    // Acquire throttling semaphore
                    await _throttleSemaphore.WaitAsync();
                    
                    // Process the item based on operation type
                    switch (item.Operation)
                    {
                        case FileOperation.ScanForViruses:
                            await ScanFileForVirusesAsync(item.FileId);
                            break;
                            
                        case FileOperation.GenerateThumbnail:
                            await GenerateThumbnailAsync(item.FileId);
                            break;
                            
                        case FileOperation.MoveToPublicStorage:
                            await MoveFileToPublicStorageAsync(item.FileId);
                            break;
                            
                        case FileOperation.MoveToPrivateStorage:
                            await MoveFileToPrivateStorageAsync(item.FileId);
                            break;
                            
                        case FileOperation.DeleteFile:
                            await DeleteFileAsync(item.FileId);
                            break;
                            
                        default:
                            _logger.LogWarning("Unknown file operation {operation} for file {fileId}", 
                                item.Operation, item.FileId);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file operation {operation} for file {fileId}", 
                        item.Operation, item.FileId);
                    
                    try
                    {
                        // Update file status to error
                        var metadata = await _metadataRepository.GetByIdAsync(item.FileId);
                        if (metadata != null)
                        {
                            metadata.Status = FileStatus.Error;
                            metadata.StatusMessage = $"Processing error: {ex.Message}";
                            metadata.LastModified = DateTime.UtcNow;
                            await _metadataRepository.UpdateAsync(metadata);
                            
                            // Publish file error event
                            await _eventBus.PublishAsync(new FileProcessingErrorEvent
                            {
                                FileId = item.FileId,
                                FileName = metadata.FileName,
                                UserId = metadata.UserId,
                                EntityId = metadata.EntityId,
                                EntityType = metadata.EntityType,
                                ErrorMessage = ex.Message,
                                Operation = item.Operation.ToString(),
                                Timestamp = DateTime.UtcNow
                            });
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Error updating file status after processing error for file {fileId}", 
                            item.FileId);
                    }
                }
                finally
                {
                    // Release throttling semaphore
                    _throttleSemaphore.Release();
                    
                    // Decrement active operations counter
                    Interlocked.Decrement(ref _activeOperations);
                }
            });
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Scans a file for viruses
        /// </summary>
        private async Task ScanFileForVirusesAsync(string fileId)
        {
            _logger.LogInformation("Scanning file {fileId} for viruses", fileId);
            
            // Get file metadata
            var metadata = await _metadataRepository.GetByIdAsync(fileId);
            if (metadata == null)
            {
                _logger.LogWarning("File {fileId} not found for virus scanning", fileId);
                return;
            }
            
            // Download file from storage
            using var fileStream = await _storageService.DownloadFileAsync(
                metadata.StorageLocation, metadata.StorageContainer);
                
            if (fileStream == null)
            {
                _logger.LogError("Could not download file {fileId} for virus scanning", fileId);
                return;
            }
            
            // Perform scan
            var scanResult = await _virusScanner.ScanFileAsync(fileStream, metadata.FileName);
            
            // Save scan result
            await _scanResultRepository.CreateAsync(scanResult);
            
            // Update file status based on scan result
            switch (scanResult.Status)
            {
                case ScanStatus.Clean:
                    metadata.Status = FileStatus.Available;
                    metadata.StatusMessage = "File scanned, no threats detected";
                    break;
                    
                case ScanStatus.Infected:
                    metadata.Status = FileStatus.Quarantined;
                    metadata.StatusMessage = $"Security threat detected: {scanResult.ThreatName ?? scanResult.StatusMessage}";
                    break;
                    
                case ScanStatus.Error:
                    metadata.Status = FileStatus.Available; // Allow access despite scan error
                    metadata.StatusMessage = "Scan incomplete, file allowed with warning";
                    _logger.LogWarning("Virus scan error for file {fileId}: {error}", fileId, scanResult.StatusMessage);
                    break;
                    
                default:
                    metadata.Status = FileStatus.Pending;
                    metadata.StatusMessage = "Scan status unresolved";
                    break;
            }
            
            metadata.LastModified = DateTime.UtcNow;
            await _metadataRepository.UpdateAsync(metadata);
            
            // Publish scan completed event
            await _eventBus.PublishAsync(new FileScannedEvent
            {
                FileId = fileId,
                FileName = metadata.FileName,
                UserId = metadata.UserId,
                EntityId = metadata.EntityId,
                EntityType = metadata.EntityType,
                ScanId = scanResult.Id,
                ScanStatus = scanResult.Status.ToString(),
                IsInfected = scanResult.Status == ScanStatus.Infected,
                ThreatName = scanResult.ThreatName,
                Timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Virus scan completed for file {fileId} with status {status}", 
                fileId, scanResult.Status);
        }

        /// <summary>
        /// Generates a thumbnail for an image file
        /// </summary>
        private async Task GenerateThumbnailAsync(string fileId)
        {
            _logger.LogInformation("Generating thumbnail for file {fileId}", fileId);
            
            // Get file metadata
            var metadata = await _metadataRepository.GetByIdAsync(fileId);
            if (metadata == null)
            {
                _logger.LogWarning("File {fileId} not found for thumbnail generation", fileId);
                return;
            }
            
            // Only process image files
            if (!metadata.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Cannot generate thumbnail for non-image file {fileId} of type {contentType}", 
                    fileId, metadata.ContentType);
                return;
            }
            
            // Generate thumbnail
            var thumbnailPath = await _storageService.GenerateThumbnailAsync(
                metadata.StorageLocation, 
                metadata.StorageContainer,
                _fileSettings.ThumbnailWidth, 
                _fileSettings.ThumbnailHeight);
                
            if (string.IsNullOrEmpty(thumbnailPath))
            {
                _logger.LogError("Failed to generate thumbnail for file {fileId}", fileId);
                return;
            }
            
            // Update metadata with thumbnail info
            metadata.HasThumbnail = true;
            metadata.ThumbnailLocation = thumbnailPath;
            metadata.LastModified = DateTime.UtcNow;
            await _metadataRepository.UpdateAsync(metadata);
            
            _logger.LogInformation("Thumbnail generated for file {fileId}", fileId);
        }

        /// <summary>
        /// Moves a file from private to public storage
        /// </summary>
        private async Task MoveFileToPublicStorageAsync(string fileId)
        {
            _logger.LogInformation("Moving file {fileId} to public storage", fileId);
            
            // Get file metadata
            var metadata = await _metadataRepository.GetByIdAsync(fileId);
            if (metadata == null)
            {
                _logger.LogWarning("File {fileId} not found for moving to public storage", fileId);
                return;
            }
            
            // Skip if already public
            if (metadata.IsPublic && metadata.StorageContainer == _fileSettings.PublicContainerName)
            {
                _logger.LogInformation("File {fileId} is already in public storage", fileId);
                return;
            }
            
            // Move file to public container
            var newLocation = await _storageService.MoveFileAsync(
                metadata.StorageLocation, 
                metadata.StorageContainer, 
                _fileSettings.PublicContainerName);
                
            if (string.IsNullOrEmpty(newLocation))
            {
                _logger.LogError("Failed to move file {fileId} to public storage", fileId);
                return;
            }
            
            // Update metadata
            metadata.IsPublic = true;
            metadata.StorageContainer = _fileSettings.PublicContainerName;
            metadata.StorageLocation = newLocation;
            metadata.LastModified = DateTime.UtcNow;
            await _metadataRepository.UpdateAsync(metadata);
            
            // Also move thumbnail if it exists
            if (metadata.HasThumbnail && !string.IsNullOrEmpty(metadata.ThumbnailLocation))
            {
                try
                {
                    // Thumbnails are usually already in public container, but ensure it
                    if (metadata.ThumbnailLocation != _fileSettings.ThumbnailContainerName)
                    {
                        await _storageService.MoveFileAsync(
                            metadata.ThumbnailLocation, 
                            _fileSettings.ThumbnailContainerName, 
                            _fileSettings.ThumbnailContainerName);
                    }
                }
                catch (Exception ex)
                {
                    // Non-fatal error, log but don't fail the operation
                    _logger.LogWarning(ex, "Error moving thumbnail for file {fileId} to public storage", fileId);
                }
            }
            
            _logger.LogInformation("File {fileId} moved to public storage successfully", fileId);
            
            // Publish file visibility changed event
            await _eventBus.PublishAsync(new FileVisibilityChangedEvent
            {
                FileId = metadata.Id,
                FileName = metadata.FileName,
                UserId = metadata.UserId,
                EntityId = metadata.EntityId,
                EntityType = metadata.EntityType,
                IsPublic = true,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Moves a file from public to private storage
        /// </summary>
        private async Task MoveFileToPrivateStorageAsync(string fileId)
        {
            _logger.LogInformation("Moving file {fileId} to private storage", fileId);
            
            // Get file metadata
            var metadata = await _metadataRepository.GetByIdAsync(fileId);
            if (metadata == null)
            {
                _logger.LogWarning("File {fileId} not found for moving to private storage", fileId);
                return;
            }
            
            // Skip if already private
            if (!metadata.IsPublic && metadata.StorageContainer == _fileSettings.PrivateContainerName)
            {
                _logger.LogInformation("File {fileId} is already in private storage", fileId);
                return;
            }
            
            // Move file to private container
            var newLocation = await _storageService.MoveFileAsync(
                metadata.StorageLocation, 
                metadata.StorageContainer, 
                _fileSettings.PrivateContainerName);
                
            if (string.IsNullOrEmpty(newLocation))
            {
                _logger.LogError("Failed to move file {fileId} to private storage", fileId);
                return;
            }
            
            // Update metadata
            metadata.IsPublic = false;
            metadata.StorageContainer = _fileSettings.PrivateContainerName;
            metadata.StorageLocation = newLocation;
            metadata.LastModified = DateTime.UtcNow;
            await _metadataRepository.UpdateAsync(metadata);
            
            _logger.LogInformation("File {fileId} moved to private storage successfully", fileId);
            
            // Publish file visibility changed event
            await _eventBus.PublishAsync(new FileVisibilityChangedEvent
            {
                FileId = metadata.Id,
                FileName = metadata.FileName,
                UserId = metadata.UserId,
                EntityId = metadata.EntityId,
                EntityType = metadata.EntityType,
                IsPublic = false,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Deletes a file and its resources
        /// </summary>
        private async Task DeleteFileAsync(string fileId)
        {
            _logger.LogInformation("Processing queued deletion for file {fileId}", fileId);
            
            // Force delete since this is a queued background operation
            await _fileService.DeleteFileAsync(fileId, null, forceDelete: true);
            
            _logger.LogInformation("Queued deletion completed for file {fileId}", fileId);
        }

        /// <summary>
        /// Called when the service is stopping
        /// </summary>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("File Processing Service is stopping");
            
            // Stop the timer
            _processingTimer?.Change(Timeout.Infinite, 0);
            
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public override void Dispose()
        {
            _processingTimer?.Dispose();
            _throttleSemaphore?.Dispose();
            
            base.Dispose();
        }
    }
}
