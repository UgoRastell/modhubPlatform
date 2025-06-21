using ModsService.Models;
using System.Collections.Generic;

namespace ModsService.Services.Versioning
{
    /// <summary>
    /// Interface pour le service de gestion des versions de mods
    /// </summary>
    public interface IVersioningService
    {
        /// <summary>
        /// Ajoute une nouvelle version à un mod
        /// </summary>
        /// <param name="mod">Le mod à mettre à jour</param>
        /// <param name="newVersion">La nouvelle version à ajouter</param>
        /// <returns>Le mod mis à jour avec la nouvelle version</returns>
        Mod AddVersion(Mod mod, ModVersion newVersion);

        /// <summary>
        /// Met à jour une version existante d'un mod
        /// </summary>
        /// <param name="mod">Le mod à mettre à jour</param>
        /// <param name="versionNumber">Le numéro de version à mettre à jour</param>
        /// <param name="updatedVersion">Les nouvelles données de version</param>
        /// <returns>Le mod mis à jour</returns>
        Mod UpdateVersion(Mod mod, string versionNumber, ModVersion updatedVersion);

        /// <summary>
        /// Supprime une version d'un mod
        /// </summary>
        /// <param name="mod">Le mod à mettre à jour</param>
        /// <param name="versionNumber">Le numéro de version à supprimer</param>
        /// <returns>Le mod mis à jour</returns>
        Mod RemoveVersion(Mod mod, string versionNumber);

        /// <summary>
        /// Obtient les notes de version pour un mod, avec possibilité de filtrer par version
        /// </summary>
        /// <param name="mod">Le mod</param>
        /// <param name="versionNumber">Numéro de version spécifique (optionnel)</param>
        /// <returns>Liste des notes de version</returns>
        List<VersionChangelogEntry> GetChangelog(Mod mod, string versionNumber = null);

        /// <summary>
        /// Formate un changelog structuré avec en-têtes Markdown
        /// </summary>
        /// <param name="mod">Le mod</param>
        /// <returns>Changelog complet au format Markdown</returns>
        string FormatFullChangelog(Mod mod);
        
        /// <summary>
        /// Parse un numéro de version au format sémantique pour permettre la comparaison
        /// </summary>
        /// <param name="version">Numéro de version</param>
        /// <returns>Version parsée</returns>
        (int Major, int Minor, int Patch, string Prerelease) ParseVersion(string version);
        
        /// <summary>
        /// Vérifie si une version est valide selon le format de versionnement sémantique
        /// </summary>
        /// <param name="version">La version à vérifier</param>
        /// <returns>True si la version est valide</returns>
        bool IsValidVersion(string version);

        /// <summary>
        /// Compare deux versions pour déterminer si la première est plus récente que la seconde
        /// </summary>
        /// <param name="version1">Première version</param>
        /// <param name="version2">Seconde version</param>
        /// <returns>True si version1 est plus récente que version2</returns>
        bool IsNewerVersion(string version1, string version2);
        
        /// <summary>
        /// Suggère un nouveau numéro de version basé sur la dernière version et le type de changement
        /// </summary>
        /// <param name="mod">Le mod</param>
        /// <param name="changeType">Type de changement (majeur, mineur, patch)</param>
        /// <returns>Suggestion de nouveau numéro de version</returns>
        string SuggestNextVersion(Mod mod, VersionChangeType changeType);
    }
    
    /// <summary>
    /// Types de changements pour suggérer une version
    /// </summary>
    public enum VersionChangeType
    {
        Major,  // Changements non rétrocompatibles
        Minor,  // Fonctionnalités ajoutées rétrocompatibles
        Patch,  // Corrections de bugs
        Beta,   // Version bêta basée sur la version actuelle
        Alpha   // Version alpha basée sur la version actuelle
    }

    /// <summary>
    /// Entrée formatée de changelog pour une version
    /// </summary>
    public class VersionChangelogEntry
    {
        public string VersionNumber { get; set; }
        public DateTime ReleaseDate { get; set; }
        public VersionType VersionType { get; set; }
        public string Changelog { get; set; }
        public bool IsRecommended { get; set; }
        public bool IsHidden { get; set; }
    }
}
