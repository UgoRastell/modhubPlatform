using FileService.Settings;
using FileService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileService.Services
{
    /// <summary>
    /// Background service that consumes mod-related events from the Mods Service
    /// Handles events like mod creation, deletion, and updates that impact file associations
    /// </summary>
    public class ModEventsConsumer : BackgroundService
    {
        private readonly IRabbitMQService _rabbitMQService;
        private readonly IFileMetadataRepository _fileRepository;
        private readonly IStorageService _storageService;
        private readonly IFileProcessingQueue _processingQueue;
        private readonly RabbitMQSettings _rabbitMQSettings;
        private readonly ILogger<ModEventsConsumer> _logger;

        public ModEventsConsumer(
            IRabbitMQService rabbitMQService,
            IFileMetadataRepository fileRepository,
            IStorageService storageService,
            IFileProcessingQueue processingQueue,
            RabbitMQSettings rabbitMQSettings,
            ILogger<ModEventsConsumer> logger)
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
            _logger.LogInformation("Mod Events Consumer started at: {time}", DateTimeOffset.Now);

            try
            {
                // Initialize RabbitMQ connections, exchanges and queues
                _rabbitMQService.EnsureInitialized();

                // Subscribe to mod deletion events
                _rabbitMQService.Subscribe<ModDeletedEvent>(
                    _rabbitMQSettings.ModExchange,
                    _rabbitMQSettings.ModEventsQueue,
                    "mod.deleted",
                    async (modDeletedEvent) => await HandleModDeletedEventAsync(modDeletedEvent));

                // Subscribe to mod update events
                _rabbitMQService.Subscribe<ModUpdatedEvent>(
                    _rabbitMQSettings.ModExchange,
                    _rabbitMQSettings.ModEventsQueue,
                    "mod.updated",
                    async (modUpdatedEvent) => await HandleModUpdatedEventAsync(modUpdatedEvent));
                
                // Subscribe to mod publication events
                _rabbitMQService.Subscribe<ModPublishedEvent>(
                    _rabbitMQSettings.ModExchange,
                    _rabbitMQSettings.ModEventsQueue,
                    "mod.published",
                    async (modPublishedEvent) => await HandleModPublishedEventAsync(modPublishedEvent));

                // Keep the service running until cancelled
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                _logger.LogInformation("Mod Events Consumer shutting down");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Mod Events Consumer");
                throw;
            }
        }

        private async Task HandleModDeletedEventAsync(ModDeletedEvent modDeletedEvent)
        {
            try
            {
                _logger.LogInformation("Processing mod deletion for mod: {modId}", modDeletedEvent.ModId);

                // Get all files associated with the mod
                var modFiles = await _fileRepository.GetByEntityIdAsync(modDeletedEvent.ModId);
                
                if (!modFiles.Any())
                {
                    _logger.LogInformation("No files found for deleted mod: {modId}", modDeletedEvent.ModId);
                    return;
                }

                _logger.LogInformation("Found {count} files for deleted mod {modId}", modFiles.Count(), modDeletedEvent.ModId);

                // Process each file based on the deletion policy
                foreach (var file in modFiles)
                {
                    if (modDeletedEvent.HardDelete)
                    {
                        // Hard delete - remove file and metadata completely
                        await _processingQueue.EnqueueFileForProcessingAsync(new FileProcessingItem
                        {
                            FileMetadataId = file.Id,
                            BlobName = file.BlobName,
                            ContainerName = file.ContainerName,
                            Operation = FileProcessingOperation.Delete,
                            UserId = file.UploadedBy,
                            ProcessingOptions = new Dictionary<string, string>
                            {
                                { "deletion_reason", "mod_deletion" },
                                { "mod_id", modDeletedEvent.ModId }
                            }
                        });
                    }
                    else
                    {
                        // Soft delete - mark files as orphaned
                        file.Metadata["mod_deleted"] = "true";
                        file.Metadata["original_mod"] = modDeletedEvent.ModId;
                        file.Metadata["marked_orphaned_at"] = DateTime.UtcNow.ToString("o");
                        await _fileRepository.UpdateAsync(file);
                    }
                }
                
                _logger.LogInformation("Completed processing file updates for deleted mod: {modId}", modDeletedEvent.ModId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling mod deleted event for mod {modId}", modDeletedEvent.ModId);
            }
        }

        private async Task HandleModUpdatedEventAsync(ModUpdatedEvent modUpdatedEvent)
        {
            try
            {
                _logger.LogInformation("Mod updated event received for mod: {modId}", modUpdatedEvent.ModId);

                // Get files associated with this mod
                var modFiles = await _fileRepository.GetByEntityIdAsync(modUpdatedEvent.ModId);
                
                if (!modFiles.Any())
                {
                    _logger.LogInformation("No files found for updated mod: {modId}", modUpdatedEvent.ModId);
                    return;
                }

                // Update file metadata with new mod information
                foreach (var file in modFiles)
                {
                    // Update metadata based on mod type
                    file.Metadata["mod_title"] = modUpdatedEvent.Title;
                    file.Metadata["mod_version"] = modUpdatedEvent.Version;
                    file.Metadata["last_mod_update"] = DateTime.UtcNow.ToString("o");
                    
                    if (modUpdatedEvent.IsPrivate != null)
                    {
                        // Update file visibility based on mod privacy setting
                        file.IsPublic = !(bool)modUpdatedEvent.IsPrivate;
                    }
                    
                    await _fileRepository.UpdateAsync(file);
                }
                
                _logger.LogInformation("Updated metadata for {count} files associated with mod {modId}", 
                    modFiles.Count(), modUpdatedEvent.ModId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling mod updated event for mod {modId}", modUpdatedEvent.ModId);
            }
        }

        private async Task HandleModPublishedEventAsync(ModPublishedEvent modPublishedEvent)
        {
            try
            {
                _logger.LogInformation("Mod published event received for mod: {modId}", modPublishedEvent.ModId);

                // Get files associated with this mod
                var modFiles = await _fileRepository.GetByEntityIdAsync(modPublishedEvent.ModId);
                
                if (!modFiles.Any())
                {
                    _logger.LogInformation("No files found for published mod: {modId}", modPublishedEvent.ModId);
                    return;
                }

                // Make files public and update metadata
                foreach (var file in modFiles)
                {
                    // Make files public since the mod is now published
                    file.IsPublic = true;
                    file.Metadata["mod_published"] = "true";
                    file.Metadata["published_at"] = modPublishedEvent.PublishedAt.ToString("o");
                    
                    await _fileRepository.UpdateAsync(file);
                    
                    // If needed, move files to public storage or update cache controls
                    await _processingQueue.EnqueueFileForProcessingAsync(new FileProcessingItem
                    {
                        FileMetadataId = file.Id,
                        BlobName = file.BlobName,
                        ContainerName = file.ContainerName,
                        Operation = FileProcessingOperation.MoveToPublic,
                        UserId = file.UploadedBy,
                        ProcessingOptions = new Dictionary<string, string>
                        {
                            { "make_public", "true" },
                            { "mod_id", modPublishedEvent.ModId }
                        }
                    });
                }
                
                _logger.LogInformation("Updated visibility for {count} files associated with published mod {modId}", 
                    modFiles.Count(), modPublishedEvent.ModId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling mod published event for mod {modId}", modPublishedEvent.ModId);
            }
        }
    }

    // Event DTOs for mod events
    public class ModDeletedEvent
    {
        public string ModId { get; set; }
        public bool HardDelete { get; set; }
        public string DeletedBy { get; set; }
        public DateTime DeletedAt { get; set; }
    }

    public class ModUpdatedEvent
    {
        public string ModId { get; set; }
        public string OwnerId { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public bool? IsPrivate { get; set; }
        public string[] Tags { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ModPublishedEvent
    {
        public string ModId { get; set; }
        public string PublishedBy { get; set; }
        public string Version { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}
