using Microsoft.AspNetCore.Http;
using ProfileService.Models;
using ProfileService.Repositories;

namespace ProfileService.Services
{
    public interface IProfileService
    {
        Task<Profile> GetProfileByIdAsync(string id);
        Task<Profile> GetProfileByUserIdAsync(string userId);
        Task<Profile> CreateProfileAsync(string userId, string username);
        Task<Profile> UpdateProfileAsync(string userId, ProfileUpdateRequest request);
        Task<string> UploadAvatarAsync(string userId, IFormFile file);
        Task<PrivacySettings> UpdatePrivacySettingsAsync(string userId, PrivacySettings settings);
        Task<bool> DeleteProfileAsync(string userId);
        Task<IEnumerable<DownloadHistoryItem>> GetDownloadHistoryAsync(string userId, int page = 1, int pageSize = 20);
        Task<ExportFile> ExportProfileDataAsync(string userId);
        Task RequestAccountDeletionAsync(string userId);
        Task<IEnumerable<Profile>> SearchProfilesAsync(string searchTerm, int page = 1, int pageSize = 20);
        Task UpdateLastActiveAsync(string userId);
    }
    
    public class ProfileUpdateRequest
    {
        public string DisplayName { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public List<SocialLink> SocialLinks { get; set; }
    }
    
    public class ExportFile
    {
        public byte[] Data { get; set; }
        public string Filename { get; set; }
        public string ContentType { get; set; } = "application/json";
    }
    
    public class ProfileSummary
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string AvatarUrl { get; set; }
        public int Level { get; set; }
        public bool IsFollowing { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
