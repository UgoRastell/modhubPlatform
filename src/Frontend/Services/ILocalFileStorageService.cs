using Microsoft.AspNetCore.Components.Forms;

namespace Frontend.Services
{
    public interface ILocalFileStorageService
    {
        Task<string> SaveModFileAsync(string modId, IBrowserFile modFile);
        Task<string> SaveThumbnailAsync(string modId, IBrowserFile thumbnailFile);
        Task<bool> DeleteModFilesAsync(string modId);
        string GetThumbnailUrl(string modId);
        string GetModDownloadUrl(string modId);
    }
}
