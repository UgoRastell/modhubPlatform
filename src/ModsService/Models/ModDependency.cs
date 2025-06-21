using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ModsService.Models
{
    /// <summary>
    /// Représente une dépendance entre mods
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ModDependency
    {
        /// <summary>
        /// Identifiant unique de la dépendance
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        
        /// <summary>
        /// ID du mod dont dépend cette version
        /// </summary>
        public string DependencyModId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du mod dépendant (pour affichage)
        /// </summary>
        public string DependencyModName { get; set; } = string.Empty;
        
        /// <summary>
        /// Version minimale requise
        /// </summary>
        public string MinimumVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Version maximale supportée (optionnel)
        /// </summary>
        public string? MaximumVersion { get; set; } = null;
        
        /// <summary>
        /// Si true, le mod ne fonctionnera pas sans cette dépendance
        /// </summary>
        public bool IsRequired { get; set; } = true;
        
        /// <summary>
        /// Notes additionnelles sur la dépendance
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        
        /// <summary>
        /// Date à laquelle cette dépendance a été définie
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de la dernière mise à jour de cette dépendance
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// URL vers une ressource externe expliquant cette dépendance (documentation, wiki, etc.)
        /// </summary>
        public string DocumentationUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Vérifie si une version spécifique est compatible avec cette dépendance
        /// </summary>
        /// <param name="version">Version à vérifier</param>
        /// <returns>True si la version est compatible</returns>
        public bool IsVersionCompatible(string version)
        {
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(MinimumVersion))
                return false;
                
            // Compare les versions (assumant un format comme "1.2.3" ou "1.2.3-beta")
            var versionComparison = CompareVersions(version, MinimumVersion);
            var isAboveMinimum = versionComparison >= 0;
            
            // Si une version maximale est spécifiée, vérifier si en dessous de celle-ci
            var isBelowMaximum = true;
            if (!string.IsNullOrEmpty(MaximumVersion))
            {
                isBelowMaximum = CompareVersions(version, MaximumVersion) <= 0;
            }
            
            return isAboveMinimum && isBelowMaximum;
        }
        
        /// <summary>
        /// Compare deux numéros de version sémantique
        /// </summary>
        /// <param name="version1">Première version</param>
        /// <param name="version2">Seconde version</param>
        /// <returns>-1 si version1 < version2, 0 si égales, 1 si version1 > version2</returns>
        private int CompareVersions(string version1, string version2)
        {
            // Extraction de la partie numérique (avant un éventuel "-beta", etc.)
            var v1Parts = GetNumericParts(version1);
            var v2Parts = GetNumericParts(version2);
            
            // Compare les segments numériques
            for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
            {
                var v1Value = i < v1Parts.Length ? v1Parts[i] : 0;
                var v2Value = i < v2Parts.Length ? v2Parts[i] : 0;
                
                if (v1Value < v2Value) return -1;
                if (v1Value > v2Value) return 1;
            }
            
            // Si les parties numériques sont identiques, compare les suffixes (alpha/beta/etc.)
            var v1Suffix = GetSuffix(version1);
            var v2Suffix = GetSuffix(version2);
            
            // Si pas de suffixes, les versions sont identiques
            if (string.IsNullOrEmpty(v1Suffix) && string.IsNullOrEmpty(v2Suffix))
                return 0;
                
            // Une version sans suffixe est supérieure à une version avec suffixe
            if (string.IsNullOrEmpty(v1Suffix)) return 1;
            if (string.IsNullOrEmpty(v2Suffix)) return -1;
            
            // Compare les suffixes lexicographiquement
            return string.Compare(v1Suffix, v2Suffix, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Extrait les parties numériques d'un numéro de version
        /// </summary>
        private int[] GetNumericParts(string version)
        {
            // Sépare la partie numérique du suffixe éventuel
            var mainVersion = version.Split('-')[0];
            
            // Découpe par les points et convertit en entiers
            var parts = mainVersion.Split('.');
            var result = new int[parts.Length];
            
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int value))
                {
                    result[i] = value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Extrait le suffixe d'un numéro de version (beta, alpha, etc.)
        /// </summary>
        private string GetSuffix(string version)
        {
            var parts = version.Split('-');
            return parts.Length > 1 ? parts[1] : string.Empty;
        }
    }
}
