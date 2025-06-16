using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using FileService.Models;
using FileService.Settings;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace FileService.Services
{
    /// <summary>
    /// Implementation of IStorageService using Azure Blob Storage
    /// </summary>
    public class AzureBlobStorageService : IStorageService
    {
        private readonly AzureStorageSettings _settings;
        private readonly ILogger<AzureBlobStorageService> _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public AzureBlobStorageService(
            AzureStorageSettings settings,
            ILogger<AzureBlobStorageService> logger)
        {
            _settings = settings;
            _logger = logger;
            _blobServiceClient = new BlobServiceClient(_settings.ConnectionString);
            
            // Ensure containers exist
            InitializeContainers();
        }

        /// <summary>
        /// Creates the default containers if they don't exist
        /// </summary>
        private void InitializeContainers()
        {
            try
            {
                // Create private container
                var privateContainer = _blobServiceClient.GetBlobContainerClient(_settings.PrivateContainerName);
                privateContainer.CreateIfNotExists(PublicAccessType.None);
                
                // Create public container
                var publicContainer = _blobServiceClient.GetBlobContainerClient(_settings.PublicContainerName);
                publicContainer.CreateIfNotExists(PublicAccessType.Blob);
                
                // Create thumbnail container
                var thumbnailContainer = _blobServiceClient.GetBlobContainerClient(_settings.ThumbnailContainerName);
                thumbnailContainer.CreateIfNotExists(PublicAccessType.Blob);
                
                _logger.LogInformation("Azure Blob Storage containers initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Azure Blob Storage containers");
                throw;
            }
        }

        /// <summary>
        /// Uploads a file to blob storage
        /// </summary>
        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string containerName = null)
        {
            try
            {
                containerName ??= _settings.PrivateContainerName;
                
                // Generate a unique blob name to avoid collisions
                var uniqueBlobName = $"{Guid.NewGuid()}_{fileName.Replace(" ", "_")}";
                
                var container = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = container.GetBlobClient(uniqueBlobName);
                
                _logger.LogInformation("Uploading file {fileName} to {containerName}/{blobName}", 
                    fileName, containerName, uniqueBlobName);

                // Upload the file with content type
                var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
                await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = blobHttpHeaders });
                
                _logger.LogInformation("Successfully uploaded file {fileName} to {containerName}/{blobName}", 
                    fileName, containerName, uniqueBlobName);
                
                return uniqueBlobName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {fileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Downloads a file from blob storage
        /// </summary>
        public async Task<Stream> DownloadFileAsync(string blobName, string containerName = null)
        {
            try
            {
                containerName ??= _settings.PrivateContainerName;
                
                var container = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = container.GetBlobClient(blobName);
                
                _logger.LogInformation("Downloading file from {containerName}/{blobName}", containerName, blobName);
                
                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogWarning("File {containerName}/{blobName} does not exist", containerName, blobName);
                    return null;
                }
                
                // Download to memory stream
                var memoryStream = new MemoryStream();
                await blobClient.DownloadToAsync(memoryStream);
                memoryStream.Position = 0;
                
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {blobName} from {containerName}", blobName, containerName);
                throw;
            }
        }

        /// <summary>
        /// Deletes a file from blob storage
        /// </summary>
        public async Task<bool> DeleteFileAsync(string blobName, string containerName = null)
        {
            try
            {
                containerName ??= _settings.PrivateContainerName;
                
                var container = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = container.GetBlobClient(blobName);
                
                _logger.LogInformation("Deleting file from {containerName}/{blobName}", containerName, blobName);
                
                var response = await blobClient.DeleteIfExistsAsync();
                
                if (response.Value)
                {
                    _logger.LogInformation("Successfully deleted file {containerName}/{blobName}", containerName, blobName);
                }
                else
                {
                    _logger.LogWarning("File {containerName}/{blobName} did not exist for deletion", containerName, blobName);
                }
                
                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {blobName} from {containerName}", blobName, containerName);
                throw;
            }
        }

        /// <summary>
        /// Generates a SAS token for direct access to a blob
        /// </summary>
        public string GenerateSasUrl(string blobName, string containerName, int expiryMinutes = 60, 
            BlobSasPermissions permissions = null)
        {
            try
            {
                containerName ??= _settings.PrivateContainerName;
                permissions ??= BlobSasPermissions.Read;
                
                var container = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = container.GetBlobClient(blobName);
                
                // Create SAS token that's valid for expiryMinutes
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = containerName,
                    BlobName = blobName,
                    Resource = "b", // b for blob
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
                };
                
                // Set permissions
                sasBuilder.SetPermissions(permissions);
                
                // Generate the SAS token
                var sasToken = blobClient.GenerateSasUri(sasBuilder).ToString();
                
                _logger.LogInformation("Generated SAS URL for {containerName}/{blobName} valid for {minutes} minutes", 
                    containerName, blobName, expiryMinutes);
                
                return sasToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SAS URL for {blobName}", blobName);
                throw;
            }
        }

        /// <summary>
        /// Moves a file between containers
        /// </summary>
        public async Task<string> MoveFileAsync(string sourceBlobName, string sourceContainerName, 
            string destinationContainerName)
        {
            try
            {
                var sourceContainer = _blobServiceClient.GetBlobContainerClient(sourceContainerName);
                var destinationContainer = _blobServiceClient.GetBlobContainerClient(destinationContainerName);
                
                var sourceBlobClient = sourceContainer.GetBlobClient(sourceBlobName);
                
                if (!await sourceBlobClient.ExistsAsync())
                {
                    _logger.LogWarning("Source blob {container}/{blob} does not exist", 
                        sourceContainerName, sourceBlobName);
                    return null;
                }
                
                // Generate a lease ID to prevent concurrent access during the copy
                var leaseClient = sourceBlobClient.GetBlobLeaseClient();
                var lease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(60));
                
                try
                {
                    // Create destination blob with the same name
                    var destBlobClient = destinationContainer.GetBlobClient(sourceBlobName);
                    
                    // Start the copy
                    var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
                    
                    // Wait for the copy to complete
                    await copyOperation.WaitForCompletionAsync();
                    
                    // Delete the source blob (with lease)
                    await sourceBlobClient.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, 
                        new BlobRequestConditions { LeaseId = lease.Value.LeaseId });
                    
                    _logger.LogInformation("Successfully moved blob from {sourceContainer}/{blob} to {destContainer}/{blob}", 
                        sourceContainerName, sourceBlobName, destinationContainerName, sourceBlobName);
                    
                    return sourceBlobName;
                }
                finally
                {
                    // Release the lease when done
                    await leaseClient.ReleaseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving blob {blob} from {sourceContainer} to {destContainer}", 
                    sourceBlobName, sourceContainerName, destinationContainerName);
                throw;
            }
        }

        /// <summary>
        /// Creates a thumbnail from an image file
        /// </summary>
        public async Task<string> GenerateThumbnailAsync(string blobName, string containerName, int maxWidth = 200, int maxHeight = 200)
        {
            try
            {
                // Download the original image
                using var originalStream = await DownloadFileAsync(blobName, containerName);
                
                if (originalStream == null)
                {
                    _logger.LogWarning("Cannot generate thumbnail - source file {container}/{blob} not found", 
                        containerName, blobName);
                    return null;
                }
                
                // Generate thumbnail name
                var thumbnailBlobName = $"thumb_{maxWidth}x{maxHeight}_{blobName}";
                
                // Check if thumbnail already exists
                var thumbnailContainer = _blobServiceClient.GetBlobContainerClient(_settings.ThumbnailContainerName);
                var thumbnailBlobClient = thumbnailContainer.GetBlobClient(thumbnailBlobName);
                
                if (await thumbnailBlobClient.ExistsAsync())
                {
                    _logger.LogInformation("Thumbnail already exists: {container}/{blob}", 
                        _settings.ThumbnailContainerName, thumbnailBlobName);
                    return thumbnailBlobName;
                }
                
                // Process the image to create thumbnail
                using var outputStream = new MemoryStream();
                using (var image = Image.Load(originalStream))
                {
                    // Resize to fit within the max dimensions while maintaining aspect ratio
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxWidth, maxHeight)
                    }));
                    
                    // Save the thumbnail
                    image.Save(outputStream, image.Metadata.DecodedImageFormat);
                }
                
                // Reset the position for reading
                outputStream.Position = 0;
                
                // Upload the thumbnail to the thumbnail container
                await thumbnailContainer.GetBlobClient(thumbnailBlobName)
                    .UploadAsync(outputStream, overwrite: true);
                
                _logger.LogInformation("Generated thumbnail {container}/{blob} for {sourceBlob}", 
                    _settings.ThumbnailContainerName, thumbnailBlobName, blobName);
                
                return thumbnailBlobName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail for {blob}", blobName);
                throw;
            }
        }

        /// <summary>
        /// Resizes an image to the specified dimensions
        /// </summary>
        public async Task<Stream> ResizeImageAsync(string blobName, string containerName, int maxWidth, int maxHeight)
        {
            try
            {
                // Download the original image
                using var originalStream = await DownloadFileAsync(blobName, containerName);
                
                if (originalStream == null)
                {
                    _logger.LogWarning("Cannot resize image - source file {container}/{blob} not found", 
                        containerName, blobName);
                    return null;
                }
                
                // Process the image to resize
                var outputStream = new MemoryStream();
                using (var image = Image.Load(originalStream))
                {
                    // Resize to fit within the max dimensions while maintaining aspect ratio
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(maxWidth, maxHeight)
                    }));
                    
                    // Save the resized image
                    image.Save(outputStream, image.Metadata.DecodedImageFormat);
                }
                
                // Reset the position for reading
                outputStream.Position = 0;
                
                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resizing image {blob}", blobName);
                throw;
            }
        }

        /// <summary>
        /// Copies a file to a new location in blob storage
        /// </summary>
        public async Task<string> CopyFileAsync(string sourceBlobName, string sourceContainerName, string destinationBlobName, 
            string destinationContainerName)
        {
            try
            {
                var sourceContainer = _blobServiceClient.GetBlobContainerClient(sourceContainerName);
                var destinationContainer = _blobServiceClient.GetBlobContainerClient(destinationContainerName);
                
                var sourceBlobClient = sourceContainer.GetBlobClient(sourceBlobName);
                
                if (!await sourceBlobClient.ExistsAsync())
                {
                    _logger.LogWarning("Source blob {container}/{blob} does not exist", 
                        sourceContainerName, sourceBlobName);
                    return null;
                }
                
                // Create destination blob
                var destBlobClient = destinationContainer.GetBlobClient(destinationBlobName ?? sourceBlobName);
                
                // Start the copy operation
                var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
                
                // Wait for the copy to complete
                await copyOperation.WaitForCompletionAsync();
                
                _logger.LogInformation("Successfully copied blob from {sourceContainer}/{sourceBlob} to {destContainer}/{destBlob}", 
                    sourceContainerName, sourceBlobName, destinationContainerName, destinationBlobName ?? sourceBlobName);
                
                return destinationBlobName ?? sourceBlobName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying blob {sourceBlob} to {destBlob}", 
                    sourceBlobName, destinationBlobName);
                throw;
            }
        }

        /// <summary>
        /// Gets properties of a blob
        /// </summary>
        public async Task<BlobProperties> GetBlobPropertiesAsync(string blobName, string containerName)
        {
            try
            {
                var container = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = container.GetBlobClient(blobName);
                
                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogWarning("Blob {container}/{blob} does not exist", containerName, blobName);
                    return null;
                }
                
                var properties = await blobClient.GetPropertiesAsync();
                return properties.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties for blob {container}/{blob}", 
                    containerName, blobName);
                throw;
            }
        }
    }
}
