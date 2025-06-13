using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Frontend.Models
{
    public class CompatibilityInfoDto : IEnumerable<KeyValuePair<string, IEnumerable<string>>>
    {
        public string? GameVersion { get; set; }
        public List<string> RequiredMods { get; set; } = new List<string>();
        public List<string> IncompatibleMods { get; set; } = new List<string>();
        public List<string> OptionalMods { get; set; } = new List<string>();
        public string? MinimumRequirements { get; set; }
        public string? RecommendedRequirements { get; set; }
        public string? CompatibilityNotes { get; set; }

        // Méthodes pour implémenter IEnumerable
        public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
        {
            if (!string.IsNullOrEmpty(GameVersion))
                yield return new KeyValuePair<string, IEnumerable<string>>("Version du jeu", new[] { GameVersion });
            
            if (RequiredMods.Any())
                yield return new KeyValuePair<string, IEnumerable<string>>("Mods requis", RequiredMods);
                
            if (IncompatibleMods.Any())
                yield return new KeyValuePair<string, IEnumerable<string>>("Mods incompatibles", IncompatibleMods);
                
            if (OptionalMods.Any())
                yield return new KeyValuePair<string, IEnumerable<string>>("Mods optionnels", OptionalMods);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        // Helper pour la méthode Any()
        public bool Any()
        {
            return !string.IsNullOrEmpty(GameVersion) ||
                   RequiredMods.Any() ||
                   IncompatibleMods.Any() ||
                   OptionalMods.Any() ||
                   !string.IsNullOrEmpty(MinimumRequirements) ||
                   !string.IsNullOrEmpty(RecommendedRequirements) ||
                   !string.IsNullOrEmpty(CompatibilityNotes);
        }
    }
}
