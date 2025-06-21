using FileService.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileService.Services
{
    /// <summary>
    /// Background service that consumes user-related events from the Identity Service
    /// Handles events like user creation, deletion, and updates that impact file ownership
    /// </summary>
    public class UserEventsConsumer : BackgroundService
    {
        private readonly IRabbitMQService _rabbitMQService;
        private readonly IFileMetadataRepository _fileRepository;
        private readonly IStorageService _storageService;
        private readonly IFileProcessingQueue _processingQueue;
        private readonly RabbitMQSettings _rabbitMQSettings;
        private readonly ILogger<UserEventsConsumer> _logger;

        public UserEventsConsumer(
            IRabbitMQService rabbitMQService,
            IFileMetadataRepository fileRepository,
            IStorageService storageService,
            IFileProcessingQueue processingQueue,
            RabbitMQSettings rabbitMQSettings,
            ILogger<UserEventsConsumer> logger)
        {
            _rabbitMQService = rabbitMQService;
            _fileRepository = fileRepository;
            _storageService = storageService;
            _processingQueue = processingQueue;
            _rabbitMQSettings = rabbitMQSettings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("User Events Consumer started at: {time}", DateTimeOffset.Now);

            try
            {
                // Initialize RabbitMQ connections, exchanges and queues
                _rabbitMQService.EnsureInitialized();

                // Subscribe to user deletion events
                _rabbitMQService.Subscribe<UserDeletedEvent>(
                    _rabbitMQSettings.UserExchange,
                    _rabbitMQSettings.UserEventsQueue,
                    "user.deleted",
                    async (userDeletedEvent) => await HandleUserDeletedEventAsync(userDeletedEvent));

                // Subscribe to user update events
                _rabbitMQService.Subscribe<UserUpdatedEvent>(
                    _rabbitMQSettings.UserExchange,
                    _rabbitMQSettings.UserEventsQueue,
                    "user.updated",
                    async (userUpdatedEvent) => await HandleUserUpdatedEventAsync(userUpdatedEvent));
                
                // Subscribe to GDPR data export requests
                _rabbitMQService.Subscribe<UserDataExportRequestedEvent>(
                    _rabbitMQSettings.UserExchange,
                    _rabbitMQSettings.UserEventsQueue,
                    "user.data.export.requested",
                    async (exportRequest) => await HandleUserDataExportRequestAsync(exportRequest));

                // Keep the service running until cancelled
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                _logger.LogInformation("User Events Consumer shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in User Events Consumer");
                throw;
            }
        }

        private async Task HandleUserDeletedEventAsync(UserDeletedEvent userDeletedEvent)
        {
            try
            {
                _logger.LogInformation("Processing user deletion for user: {userId}", userDeletedEvent.UserId);

                // Get all files uploaded by the user
                var userFiles = await _fileRepository.GetByUserIdAsync(userDeletedEvent.UserId);
                
                if (!userFiles.Any())
                {
                    _logger.LogInformation("No files found for deleted user: {userId}", userDeletedEvent.UserId);
                    return;
                }

                _logger.LogInformation("Found {count} files for deleted user {userId}", userFiles.Count(), userDeletedEvent.UserId);

                // Process user deletion based on the deletion policy
                if (userDeletedEvent.HardDelete)
                {
                    // Hard delete - remove all user files from storage
                    foreach (var file in userFiles)
                    {
                        // Queue up the file for deletion
                        await _processingQueue.EnqueueFileForProcessingAsync(new FileProcessingItem
                        {
                            FileMetadataId = file.Id,
                            BlobName = file.BlobName,
                            ContainerName = file.ContainerName,
                            Operation = FileProcessingOperation.Delete,
                            UserId = userDeletedEvent.UserId,
                            ProcessingOptions = new Dictionary<string, string>
                            {
                                { "deletion_reason", "user_deletion" },
                                { "delete_metadata", "true" }
                            }
                        });
                    }

                    _logger.LogInformation("Queued {count} files for hard deletion for user {userId}", 
                        userFiles.Count(), userDeletedEvent.UserId);
                }
                else
                {
                    // Soft delete - anonymize file ownership
                    var anonymizedCount = await _fileRepository.AnonymizeUserFilesAsync(userDeletedEvent.UserId);
                    
                    _logger.LogInformation("Anonymized {count} files for deleted user {userId}", 
                        anonymizedCount, userDeletedEvent.UserId);
                }
                
                _logger.LogInformation("Completed processing user deletion for user: {userId}", userDeletedEvent.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user deleted event for user {userId}", userDeletedEvent.UserId);
            }
        }

        private async Task HandleUserUpdatedEventAsync(UserUpdatedEvent userUpdatedEvent)
        {
            try
            {
                _logger.LogInformation("Processing user update for user: {userId}", userUpdatedEvent.UserId);

                // If username changed, update file metadata for this user
                if (!string.IsNullOrEmpty(userUpdatedEvent.Username))
                {
                    // Get all files uploaded by the user
                    var userFiles = await _fileRepository.GetByUserIdAsync(userUpdatedEvent.UserId);
                    
                    if (!userFiles.Any())
                    {
                        _logger.LogInformation("No files found for updated user: {userId}", userUpdatedEvent.UserId);
                        return;
                    }

                    // Update metadata with new username
                    foreach (var file in userFiles)
                    {
                        file.UploadedByUsername = userUpdatedEvent.Username;
                        await _fileRepository.UpdateAsync(file);
                    }
                    
                    _logger.LogInformation("Updated username on {count} files for user {userId}", 
                        userFiles.Count(), userUpdatedEvent.UserId);
                }
                
                _logger.LogInformation("Completed processing user update for user: {userId}", userUpdatedEvent.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user updated event for user {userId}", userUpdatedEvent.UserId);
            }
        }

        private async Task HandleUserDataExportRequestAsync(UserDataExportRequestedEvent exportRequest)
        {
            try
            {
                _logger.LogInformation("Processing data export request for user: {userId}", exportRequest.UserId);

                // Get all files uploaded by the user
                var userFiles = await _fileRepository.GetByUserIdAsync(exportRequest.UserId);
                
                if (!userFiles.Any())
                {
                    _logger.LogInformation("No files found for user data export: {userId}", exportRequest.UserId);
                    
                    // Send empty result back to the requesting service
                    await _rabbitMQService.PublishAsync(
                        _rabbitMQSettings.FileExchange,
                        "file.user.export.result",
                        new UserFileExportResultEvent
                        {
                            UserId = exportRequest.UserId,
                            RequestId = exportRequest.RequestId,
                            Files = new List<FileExportData>()
                        });
                    
                    return;
                }

                // Prepare file export data
                var fileExportData = userFiles.Select(file => new FileExportData
                {
                    Id = file.Id,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.FileSize,
                    UploadedAt = file.UploadedAt,
                    IsPublic = file.IsPublic,
                    EntityId = file.EntityId,
                    EntityType = file.EntityType,
                    FileCategory = file.FileCategory,
                    MetadataSummary = file.GetMetadataSummary()
                }).ToList();
                
                // Send the export data back to the requesting service
                await _rabbitMQService.PublishAsync(
                    _rabbitMQSettings.FileExchange,
                    "file.user.export.result",
                    new UserFileExportResultEvent
                    {
                        UserId = exportRequest.UserId,
                        RequestId = exportRequest.RequestId,
                        Files = fileExportData
                    });
                
                _logger.LogInformation("Completed data export for user {userId} with {count} files", 
                    exportRequest.UserId, fileExportData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user data export request for user {userId}", exportRequest.UserId);
                
                // Send error response
                try
                {
                    await _rabbitMQService.PublishAsync(
                        _rabbitMQSettings.FileExchange,
                        "file.user.export.result",
                        new UserFileExportResultEvent
                        {
                            UserId = exportRequest.UserId,
                            RequestId = exportRequest.RequestId,
                            Error = ex.Message
                        });
                }
                catch (Exception publishEx)
                {
                    _logger.LogError(publishEx, "Failed to publish error response for data export");
                }
            }
        }
    }

    // Event DTOs for user events
    public class UserDeletedEvent
    {
        public string UserId { get; set; }
        public bool HardDelete { get; set; }
        public DateTime DeletedAt { get; set; }
    }

    public class UserUpdatedEvent
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class UserDataExportRequestedEvent
    {
        public string UserId { get; set; }
        public string RequestId { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class UserFileExportResultEvent
    {
        public string UserId { get; set; }
        public string RequestId { get; set; }
        public List<FileExportData> Files { get; set; } = new List<FileExportData>();
        public string Error { get; set; }
    }

    public class FileExportData
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsPublic { get; set; }
        public string EntityId { get; set; }
        public string EntityType { get; set; }
        public string FileCategory { get; set; }
        public Dictionary<string, string> MetadataSummary { get; set; }
    }
}
