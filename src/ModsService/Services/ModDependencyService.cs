using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModsService.Models;
using ModsService.Repositories;

namespace ModsService.Services
{
    /// <summary>
    /// Service de gestion des dépendances entre mods
    /// </summary>
    public class ModDependencyService : IModDependencyService
    {
        private readonly ILogger<ModDependencyService> _logger;
        private readonly ModRepository _modRepository;
        
        public ModDependencyService(
            ILogger<ModDependencyService> logger,
            ModRepository modRepository)
        {
            _logger = logger;
            _modRepository = modRepository;
        }
        
        /// <summary>
        /// Vérifie si toutes les dépendances d'un mod sont satisfaites
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version</param>
        /// <returns>Résultat de la vérification des dépendances</returns>
        public async Task<DependencyCheckResult> CheckDependenciesAsync(string modId, string versionNumber)
        {
            var result = new DependencyCheckResult
            {
                IsSatisfied = true,
                MissingDependencies = new List<ModDependency>(),
                IncompatibleDependencies = new List<ModCompatibility>()
            };
            
            try
            {
                var mod = await _modRepository.GetModByIdAsync(modId);
                if (mod == null)
                {
                    _logger.LogWarning("Mod non trouvé avec ID {ModId}", modId);
                    result.IsSatisfied = false;
                    return result;
                }
                
                var version = mod.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
                if (version == null)
                {
                    _logger.LogWarning("Version {Version} non trouvée pour le mod {ModId}", versionNumber, modId);
                    result.IsSatisfied = false;
                    return result;
                }
                
                // Vérifier chaque dépendance
                foreach (var dependency in version.Dependencies)
                {
                    var dependencyMod = await _modRepository.GetModByIdAsync(dependency.DependencyModId);
                    if (dependencyMod == null)
                    {
                        _logger.LogWarning("Mod dépendant non trouvé: {DependencyModId}", dependency.DependencyModId);
                        result.IsSatisfied = false;
                        result.MissingDependencies.Add(dependency);
                        continue;
                    }
                    
                    // Vérifier si au moins une version du mod de dépendance est compatible
                    bool versionFound = false;
                    foreach (var depVersion in dependencyMod.Versions)
                    {
                        if (dependency.IsVersionCompatible(depVersion.VersionNumber))
                        {
                            versionFound = true;
                            break;
                        }
                    }
                    
                    if (!versionFound && dependency.IsRequired)
                    {
                        _logger.LogWarning("Aucune version compatible pour la dépendance {DependencyModId}", dependency.DependencyModId);
                        result.IsSatisfied = false;
                        result.MissingDependencies.Add(dependency);
                    }
                }
                
                // Vérifier les incompatibilités déclarées
                foreach (var compatibility in version.Compatibilities)
                {
                    if (compatibility.Type == CompatibilityType.Incompatible)
                    {
                        var targetMod = await _modRepository.GetModByIdAsync(compatibility.TargetModId);
                        if (targetMod != null && targetMod.Versions.Any(v => compatibility.TargetVersions.Contains(v.VersionNumber)))
                        {
                            _logger.LogWarning("Incompatibilité détectée avec {TargetModId}", compatibility.TargetModId);
                            result.IsSatisfied = false;
                            result.IncompatibleDependencies.Add(compatibility);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des dépendances pour {ModId} version {Version}", modId, versionNumber);
                result.IsSatisfied = false;
                result.ErrorMessage = $"Erreur interne: {ex.Message}";
            }
            
            return result;
        }
        
        /// <summary>
        /// Analyse une nouvelle version pour détecter automatiquement les compatibilités potentielles
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version</param>
        /// <returns>Liste des compatibilités analysées</returns>
        public async Task<List<ModCompatibility>> AnalyzeCompatibilitiesAsync(string modId, string versionNumber)
        {
            var compatibilities = new List<ModCompatibility>();
            
            try
            {
                var mod = await _modRepository.GetModByIdAsync(modId);
                if (mod == null || !mod.Versions.Any(v => v.VersionNumber == versionNumber))
                {
                    _logger.LogWarning("Mod ou version non trouvé: {ModId}, {Version}", modId, versionNumber);
                    return compatibilities;
                }
                
                // Analyser les mods de la même catégorie
                var categoryMods = await _modRepository.GetModsByCategoryAsync(mod.CategoryId);
                
                foreach (var categoryMod in categoryMods.Where(m => m.Id != modId))
                {
                    // Analyser les mods associés au même jeu
                    if (categoryMod.GameId == mod.GameId)
                    {
                        var compatibility = new ModCompatibility
                        {
                            SourceModId = modId,
                            TargetModId = categoryMod.Id,
                            TargetModName = categoryMod.Name,
                            Type = CompatibilityType.PartiallyCompatible, // Par défaut, supposer partiellement compatible
                            SourceVersions = new List<string> { versionNumber },
                            Notes = "Compatibilité automatiquement détectée (même catégorie et jeu). À vérifier par l'utilisateur."
                        };
                        
                        compatibilities.Add(compatibility);
                    }
                }
                
                // D'autres règles d'analyse pourraient être ajoutées ici...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'analyse de compatibilité pour {ModId} version {Version}", modId, versionNumber);
            }
            
            return compatibilities;
        }
    }
    
    /// <summary>
    /// Interface du service de gestion des dépendances
    /// </summary>
    public interface IModDependencyService
    {
        /// <summary>
        /// Vérifie si toutes les dépendances d'un mod sont satisfaites
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version</param>
        /// <returns>Résultat de la vérification des dépendances</returns>
        Task<DependencyCheckResult> CheckDependenciesAsync(string modId, string versionNumber);
        
        /// <summary>
        /// Analyse une nouvelle version pour détecter automatiquement les compatibilités potentielles
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version</param>
        /// <returns>Liste des compatibilités analysées</returns>
        Task<List<ModCompatibility>> AnalyzeCompatibilitiesAsync(string modId, string versionNumber);
    }
    
    /// <summary>
    /// Résultat de la vérification des dépendances
    /// </summary>
    public class DependencyCheckResult
    {
        /// <summary>
        /// True si toutes les dépendances sont satisfaites
        /// </summary>
        public bool IsSatisfied { get; set; }
        
        /// <summary>
        /// Liste des dépendances manquantes
        /// </summary>
        public List<ModDependency> MissingDependencies { get; set; } = new List<ModDependency>();
        
        /// <summary>
        /// Liste des incompatibilités détectées
        /// </summary>
        public List<ModCompatibility> IncompatibleDependencies { get; set; } = new List<ModCompatibility>();
        
        /// <summary>
        /// Message d'erreur en cas de problème technique
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
