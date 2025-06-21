using ProfileService.Repositories;

namespace ProfileService.Services
{
    public interface IPreferenceService
    {
        Task<UserPreferences> GetPreferencesAsync(string userId);
        Task<UserPreferences> UpdatePreferencesAsync(string userId, UserPreferences preferences);
        Task ResetToDefaultsAsync(string userId);
        Task AddFavoriteTagsAsync(string userId, List<string> tags);
        Task RemoveFavoriteTagsAsync(string userId, List<string> tags);
        Task AddFavoriteGamesAsync(string userId, List<string> games);
        Task RemoveFavoriteGamesAsync(string userId, List<string> games);
        Task UpdateNotificationSettingsAsync(string userId, NotificationPreferences notificationSettings);
        Task UpdateUiSettingsAsync(string userId, UiPreferences uiSettings);
        Task<bool> DeletePreferencesAsync(string userId);
    }
}
