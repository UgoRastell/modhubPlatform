using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Frontend.Models.Settings;
using Microsoft.Extensions.Configuration;

namespace Frontend.Services
{
    /// <summary>
    /// Service pour gérer les paramètres utilisateur
    /// </summary>
    public interface IUserSettingsService
    {
        /// <summary>
        /// Récupère tous les paramètres de l'utilisateur
        /// </summary>
        Task<UserSettingsModel> GetAllSettingsAsync();
        
        /// <summary>
        /// Met à jour les paramètres de profil
        /// </summary>
        Task<bool> UpdateProfileAsync(ProfileSettings profile);
        
        /// <summary>
        /// Met à jour les paramètres de sécurité
        /// </summary>
        Task<bool> UpdateSecurityAsync(SecuritySettings security);
        
        /// <summary>
        /// Change le mot de passe de l'utilisateur
        /// </summary>
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
        
        /// <summary>
        /// Met à jour les paramètres de notifications
        /// </summary>
        Task<bool> UpdateNotificationsAsync(NotificationSettings notifications);
        
        /// <summary>
        /// Met à jour les paramètres de confidentialité
        /// </summary>
        Task<bool> UpdatePrivacyAsync(PrivacySettings privacy);
        
        /// <summary>
        /// Met à jour les paramètres d'affichage
        /// </summary>
        Task<bool> UpdateDisplaySettingsAsync(DisplaySettings display);
        
        /// <summary>
        /// Met à jour les paramètres d'intégration
        /// </summary>
        Task<bool> UpdateIntegrationsAsync(IntegrationSettings integrations);
        
        /// <summary>
        /// Télécharge l'avatar utilisateur
        /// </summary>
        Task<string> UploadAvatarAsync(byte[] imageData, string fileName);
        
        /// <summary>
        /// Demande l'exportation des données utilisateur
        /// </summary>
        Task<bool> RequestDataExportAsync();
        
        /// <summary>
        /// Demande la suppression du compte utilisateur
        /// </summary>
        Task<bool> RequestAccountDeletionAsync();
        
        /// <summary>
        /// Désactive tous les mods d'un créateur
        /// </summary>
        Task<bool> DisableAllModsAsync(bool disable);
        
        /// <summary>
        /// Réinitialise toutes les clés API
        /// </summary>
        Task<bool> ResetAllApiKeysAsync();
        
        /// <summary>
        /// Connecte le compte à un service externe
        /// </summary>
        Task<bool> ConnectExternalServiceAsync(string serviceName);
        
        /// <summary>
        /// Déconnecte un service externe
        /// </summary>
        Task<bool> DisconnectExternalServiceAsync(string serviceName, string integrationId);
        
        /// <summary>
        /// Révoque une session active
        /// </summary>
        Task<bool> RevokeSessionAsync(string sessionId);
    }

    public class UserSettingsService : IUserSettingsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = string.Empty;

        public UserSettingsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiSettings:GatewayUrl"] ?? "https://modhub.ovh";
        }

        public async Task<UserSettingsModel> GetAllSettingsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/users/me/settings");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserSettingsModel>() 
                        ?? new UserSettingsModel();
                }
                
                throw new Exception($"Erreur lors de la récupération des paramètres: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans GetAllSettingsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateProfileAsync(ProfileSettings profile)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/api/users/me/settings/profile", profile);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans UpdateProfileAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateSecurityAsync(SecuritySettings security)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/api/users/me/settings/security", security);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans UpdateSecurityAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                var passwordChange = new
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                };
                
                var response = await _httpClient.PatchAsJsonAsync(
                    $"{_apiBaseUrl}/api/users/me/settings/security/password", 
                    passwordChange);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans ChangePasswordAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateNotificationsAsync(NotificationSettings notifications)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiBaseUrl}/api/users/me/settings/notifications", 
                    notifications);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans UpdateNotificationsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdatePrivacyAsync(PrivacySettings privacy)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiBaseUrl}/api/users/me/settings/privacy", 
                    privacy);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans UpdatePrivacyAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateDisplaySettingsAsync(DisplaySettings display)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiBaseUrl}/api/users/me/settings/display", 
                    display);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans UpdateDisplaySettingsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateIntegrationsAsync(IntegrationSettings integrations)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiBaseUrl}/api/users/me/settings/integrations", 
                    integrations);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans UpdateIntegrationsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<string> UploadAvatarAsync(byte[] imageData, string fileName)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(imageData);
                content.Add(fileContent, "file", fileName);

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/users/me/settings/avatar", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AvatarUploadResult>();
                    return result?.AvatarUrl ?? string.Empty;
                }
                
                throw new Exception($"Erreur lors du téléchargement de l'avatar: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans UploadAvatarAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RequestDataExportAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/users/me/data-export", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans RequestDataExportAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RequestAccountDeletionAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/users/me/account-deletion", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans RequestAccountDeletionAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DisableAllModsAsync(bool disable)
        {
            try
            {
                var request = new { Disabled = disable };
                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/users/me/mods/disable-all", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans DisableAllModsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ResetAllApiKeysAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/users/me/settings/security/api-keys/reset", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans ResetAllApiKeysAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ConnectExternalServiceAsync(string serviceName)
        {
            try
            {
                var request = new { ServiceName = serviceName };
                var response = await _httpClient.PostAsJsonAsync($"{_apiBaseUrl}/api/users/me/settings/integrations/connect", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans ConnectExternalServiceAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DisconnectExternalServiceAsync(string serviceName, string integrationId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/users/me/settings/integrations/{serviceName}/{integrationId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans DisconnectExternalServiceAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RevokeSessionAsync(string sessionId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/api/users/me/settings/security/sessions/{sessionId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception dans RevokeSessionAsync: {ex.Message}");
                throw;
            }
        }
    }

    public class AvatarUploadResult
    {
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
