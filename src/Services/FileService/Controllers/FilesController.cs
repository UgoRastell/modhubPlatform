using FileService.Models;
using FileService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IFileService fileService, ILogger<FilesController> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFileMetadata(string id)
        {
            var metadata = await _fileService.GetFileMetadataAsync(id);
            
            if (metadata == null)
            {
                return NotFound();
            }

            if (!metadata.IsPublic)
            {
                // Check if user is authenticated and authorized to see this file
                if (!User.Identity.IsAuthenticated)
                {
                    return Unauthorized("Authentication required to view this file");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var validationResult = await _fileService.ValidateFilePermissionsAsync(id, userId, FileAction.View);

                if (!validationResult.IsAllowed)
                {
                    return Forbid(validationResult.Message);
                }
            }

            return Ok(metadata);
        }

        [HttpGet("download/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadFile(string id, [FromQuery] bool direct = false)
        {
            var metadata = await _fileService.GetFileMetadataAsync(id);
            
            if (metadata == null)
            {
                return NotFound();
            }

            if (metadata.Status != FileStatus.Available)
            {
                return BadRequest($"File is not available. Current status: {metadata.Status}");
            }

            if (!metadata.IsPublic)
            {
                // Check if user is authenticated and authorized to download this file
                if (!User.Identity.IsAuthenticated)
                {
                    return Unauthorized("Authentication required to download this file");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var validationResult = await _fileService.ValidateFilePermissionsAsync(id, userId, FileAction.Download);

                if (!validationResult.IsAllowed)
                {
                    return Forbid(validationResult.Message);
                }
            }

            // Record download statistics
            await _fileService.RecordFileAccessAsync(id);

            if (direct)
            {
                // Direct download
                var fileStream = await _fileService.DownloadFileAsync(id);
                return File(fileStream, metadata.ContentType, metadata.FileName);
            }
            else
            {
                // Generate pre-signed URL
                var url = await _fileService.GetDownloadUrlAsync(id, TimeSpan.FromHours(1));
                return Ok(new { DownloadUrl = url });
            }
        }

        [HttpGet("image/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetImage(string id, [FromQuery] int? width = null, [FromQuery] int? height = null)
        {
            var metadata = await _fileService.GetFileMetadataAsync(id);
            
            if (metadata == null)
            {
                return NotFound();
            }

            // Check if this is actually an image
            if (metadata.FileType != FileType.ModImage && 
                metadata.FileType != FileType.ProfileAvatar && 
                metadata.FileType != FileType.BannerImage && 
                metadata.FileType != FileType.ScreenshotImage)
            {
                return BadRequest("The requested file is not an image");
            }

            if (metadata.Status != FileStatus.Available)
            {
                return BadRequest($"Image is not available. Current status: {metadata.Status}");
            }

            if (!metadata.IsPublic)
            {
                // Check if user is authenticated and authorized to view this image
                if (!User.Identity.IsAuthenticated)
                {
                    return Unauthorized("Authentication required to view this image");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var validationResult = await _fileService.ValidateFilePermissionsAsync(id, userId, FileAction.View);

                if (!validationResult.IsAllowed)
                {
                    return Forbid(validationResult.Message);
                }
            }

            // Record access statistics
            await _fileService.RecordFileAccessAsync(id);

            // Get image URL, potentially with resizing
            var imageUrl = await _fileService.GetImageUrlAsync(id, width, height);
            return Ok(new { ImageUrl = imageUrl });
        }

        [HttpGet("thumbnail/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetThumbnail(string id)
        {
            var metadata = await _fileService.GetFileMetadataAsync(id);
            
            if (metadata == null)
            {
                return NotFound();
            }

            // Check if this is actually an image
            if (metadata.FileType != FileType.ModImage && 
                metadata.FileType != FileType.ProfileAvatar && 
                metadata.FileType != FileType.BannerImage && 
                metadata.FileType != FileType.ScreenshotImage)
            {
                return BadRequest("The requested file is not an image");
            }

            // Thumbnails are generally public even for private images
            var thumbnailUrl = await _fileService.GetThumbnailUrlAsync(id);
            
            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                return NotFound("Thumbnail not available");
            }
            
            return Ok(new { ThumbnailUrl = thumbnailUrl });
        }

        [HttpGet("entity/{entityId}")]
        public async Task<IActionResult> GetFilesByEntityId(string entityId)
        {
            var files = await _fileService.GetFilesByEntityIdAsync(entityId);
            return Ok(files);
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] FileType fileType, [FromQuery] string relatedEntityId = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was uploaded");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _fileService.UploadFileAsync(file, fileType, userId, relatedEntityId);

            if (!result.Success)
            {
                return BadRequest(new { Errors = result.Errors });
            }

            return Ok(new { 
                FileId = result.FileId, 
                Url = result.Url, 
                Status = result.Status.ToString() 
            });
        }

        [HttpPost("upload/url")]
        [Authorize]
        public async Task<IActionResult> GetUploadUrl(
            [FromQuery] string fileName, 
            [FromQuery] FileType fileType, 
            [FromQuery] string relatedEntityId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var uploadUrl = await _fileService.GenerateUploadUrlAsync(fileName, fileType, userId, relatedEntityId);

            return Ok(new { UploadUrl = uploadUrl });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteFile(string id)
        {
            var metadata = await _fileService.GetFileMetadataAsync(id);
            
            if (metadata == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var validationResult = await _fileService.ValidateFilePermissionsAsync(id, userId, FileAction.Delete);

            if (!validationResult.IsAllowed)
            {
                return Forbid(validationResult.Message);
            }

            var result = await _fileService.DeleteFileAsync(id);

            if (!result)
            {
                return StatusCode(500, "Failed to delete the file");
            }

            return NoContent();
        }

        [HttpGet("scan/{id}")]
        [Authorize]
        public async Task<IActionResult> GetFileScanResult(string id)
        {
            var metadata = await _fileService.GetFileMetadataAsync(id);
            
            if (metadata == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var validationResult = await _fileService.ValidateFilePermissionsAsync(id, userId, FileAction.View);

            if (!validationResult.IsAllowed)
            {
                return Forbid(validationResult.Message);
            }

            var scanResult = await _fileService.GetFileScanResultAsync(id);
            
            if (scanResult == null)
            {
                return NotFound("Scan result not found");
            }

            return Ok(scanResult);
        }

        [HttpPut("{id}/metadata")]
        [Authorize]
        public async Task<IActionResult> UpdateFileMetadata(string id, [FromBody] Dictionary<string, string> metadata)
        {
            var fileMetadata = await _fileService.GetFileMetadataAsync(id);
            
            if (fileMetadata == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var validationResult = await _fileService.ValidateFilePermissionsAsync(id, userId, FileAction.Update);

            if (!validationResult.IsAllowed)
            {
                return Forbid(validationResult.Message);
            }

            var result = await _fileService.UpdateFileMetadataAsync(id, metadata);

            if (!result)
            {
                return StatusCode(500, "Failed to update file metadata");
            }

            return Ok();
        }

        [HttpPost("batch")]
        [Authorize]
        public async Task<IActionResult> BatchGetMetadata([FromBody] List<string> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest("No file IDs provided");
            }

            var files = await _fileService.BatchGetMetadataAsync(ids);
            return Ok(files);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchFiles(
            [FromQuery] string searchTerm, 
            [FromQuery] FileType? fileType = null, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest("Search term is required");
            }

            var results = await _fileService.SearchFilesAsync(searchTerm, fileType, page, pageSize);
            return Ok(results);
        }
    }
}
