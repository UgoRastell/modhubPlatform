using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Frontend.Models
{
    /// <summary>
    /// Informations de compatibilité d'un mod, optimisée pour la sérialisation JSON
    /// </summary>
    public class CompatibilityInfoDto
    {
        [JsonPropertyName("gameVersion")]
        public string? GameVersion { get; set; }
        
        [JsonPropertyName("requiredMods")]
        public List<string> RequiredMods { get; set; } = new List<string>();
        
        [JsonPropertyName("incompatibleMods")]
        public List<string> IncompatibleMods { get; set; } = new List<string>();
        
        [JsonPropertyName("optionalMods")]
        public List<string> OptionalMods { get; set; } = new List<string>();
        
        [JsonPropertyName("minimumRequirements")]
        public string? MinimumRequirements { get; set; }
        
        [JsonPropertyName("recommendedRequirements")]
        public string? RecommendedRequirements { get; set; }
        
        [JsonPropertyName("compatibilityNotes")]
        public string? CompatibilityNotes { get; set; }
        
        /// <summary>
        /// Vérifie si des informations de compatibilité sont disponibles
        /// </summary>
        public bool HasCompatibilityInfo()
        {
            return !string.IsNullOrEmpty(GameVersion) ||
                   RequiredMods.Any() ||
                   IncompatibleMods.Any() ||
                   OptionalMods.Any() ||
                   !string.IsNullOrEmpty(MinimumRequirements) ||
                   !string.IsNullOrEmpty(RecommendedRequirements) ||
                   !string.IsNullOrEmpty(CompatibilityNotes);
        }
        
        /// <summary>
        /// Obtient un dictionnaire des informations de compatibilité pour l'affichage
        /// </summary>
        public Dictionary<string, List<string>> GetCompatibilityData()
        {
            var result = new Dictionary<string, List<string>>();
            
            if (!string.IsNullOrEmpty(GameVersion))
                result["Version du jeu"] = new List<string> { GameVersion };
            
            if (RequiredMods.Any())
                result["Mods requis"] = RequiredMods;
                
            if (IncompatibleMods.Any())
                result["Mods incompatibles"] = IncompatibleMods;
                
            if (OptionalMods.Any())
                result["Mods optionnels"] = OptionalMods;
                
            return result;
        }
    }
}
