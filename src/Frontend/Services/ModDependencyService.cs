using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Frontend.Models;
using Microsoft.Extensions.Logging;

namespace Frontend.Services
{
    public class ModDependencyService : IModDependencyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ModDependencyService> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public ModDependencyService(HttpClient httpClient, ILogger<ModDependencyService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Récupère les dépendances d'un mod
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version (optionnel)</param>
        /// <returns>Liste des dépendances</returns>
        public async Task<List<ModDependencyDto>> GetModDependenciesAsync(string modId, string? versionNumber = null)
        {
            try
            {
                string url = $"api/mods/{modId}/dependencies";
                if (!string.IsNullOrEmpty(versionNumber))
                {
                    url += $"?version={versionNumber}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<ModDependencyDto>>(_jsonOptions) 
                    ?? new List<ModDependencyDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des dépendances pour le mod {ModId}", modId);
                return new List<ModDependencyDto>();
            }
        }

        /// <summary>
        /// Ajoute une dépendance à un mod
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version</param>
        /// <param name="dependency">Dépendance à ajouter</param>
        /// <returns>Résultat de l'opération</returns>
        public async Task<bool> AddModDependencyAsync(string modId, string versionNumber, ModDependencyDto dependency)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"api/mods/{modId}/versions/{versionNumber}/dependencies", 
                    dependency);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout d'une dépendance au mod {ModId}", modId);
                return false;
            }
        }

        /// <summary>
        /// Supprime une dépendance d'un mod
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version</param>
        /// <param name="dependencyId">ID de la dépendance à supprimer</param>
        /// <returns>Résultat de l'opération</returns>
        public async Task<bool> DeleteModDependencyAsync(string modId, string versionNumber, string dependencyId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(
                    $"api/mods/{modId}/versions/{versionNumber}/dependencies/{dependencyId}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression d'une dépendance du mod {ModId}", modId);
                return false;
            }
        }

        /// <summary>
        /// Récupère les compatibilités d'un mod
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version (optionnel)</param>
        /// <returns>Liste des compatibilités</returns>
        public async Task<List<ModCompatibilityDto>> GetModCompatibilitiesAsync(string modId, string? versionNumber = null)
        {
            try
            {
                string url = $"api/mods/{modId}/compatibilities";
                if (!string.IsNullOrEmpty(versionNumber))
                {
                    url += $"?version={versionNumber}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<ModCompatibilityDto>>(_jsonOptions) 
                    ?? new List<ModCompatibilityDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des compatibilités pour le mod {ModId}", modId);
                return new List<ModCompatibilityDto>();
            }
        }

        /// <summary>
        /// Ajoute une compatibilité à un mod
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="compatibility">Compatibilité à ajouter</param>
        /// <returns>Résultat de l'opération</returns>
        public async Task<bool> AddModCompatibilityAsync(string modId, ModCompatibilityDto compatibility)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"api/mods/{modId}/compatibilities", 
                    compatibility);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout d'une compatibilité au mod {ModId}", modId);
                return false;
            }
        }

        /// <summary>
        /// Met à jour une compatibilité
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="compatibilityId">ID de la compatibilité</param>
        /// <param name="compatibility">Données mises à jour</param>
        /// <returns>Résultat de l'opération</returns>
        public async Task<bool> UpdateModCompatibilityAsync(string modId, string compatibilityId, ModCompatibilityDto compatibility)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"api/mods/{modId}/compatibilities/{compatibilityId}", 
                    compatibility);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour d'une compatibilité pour le mod {ModId}", modId);
                return false;
            }
        }

        /// <summary>
        /// Vérifie si un mod est compatible avec l'environnement de l'utilisateur
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version</param>
        /// <returns>Résultat de la vérification</returns>
        public async Task<ModCompatibilityCheckDto> CheckCompatibilityAsync(string modId, string versionNumber)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/mods/{modId}/versions/{versionNumber}/compatibility-check");
                
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadFromJsonAsync<ModCompatibilityCheckDto>(_jsonOptions) 
                    ?? new ModCompatibilityCheckDto { IsCompatible = false, Message = "Erreur de communication avec le serveur" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de compatibilité pour {ModId} version {Version}", modId, versionNumber);
                return new ModCompatibilityCheckDto
                {
                    IsCompatible = false,
                    Message = $"Erreur lors de la vérification: {ex.Message}"
                };
            }
        }
    }

    public interface IModDependencyService
    {
        Task<List<ModDependencyDto>> GetModDependenciesAsync(string modId, string? versionNumber = null);
        Task<bool> AddModDependencyAsync(string modId, string versionNumber, ModDependencyDto dependency);
        Task<bool> DeleteModDependencyAsync(string modId, string versionNumber, string dependencyId);
        Task<List<ModCompatibilityDto>> GetModCompatibilitiesAsync(string modId, string? versionNumber = null);
        Task<bool> AddModCompatibilityAsync(string modId, ModCompatibilityDto compatibility);
        Task<bool> UpdateModCompatibilityAsync(string modId, string compatibilityId, ModCompatibilityDto compatibility);
        Task<ModCompatibilityCheckDto> CheckCompatibilityAsync(string modId, string versionNumber);
    }

    public class ModCompatibilityCheckDto
    {
        public bool IsCompatible { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ModDependencyDto> MissingDependencies { get; set; } = new List<ModDependencyDto>();
        public List<ModCompatibilityDto> IncompatibleMods { get; set; } = new List<ModCompatibilityDto>();
    }
}
