using System;
using MongoDB.Bson.Serialization.Attributes;

namespace ModsService.Models
{
    /// <summary>
    /// Représente un fichier associé à une version d'un mod
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ModFile
    {
        /// <summary>
        /// Identifiant unique du fichier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Nom du fichier original
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Type MIME du fichier
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
        
        /// <summary>
        /// Taille du fichier en bytes
        /// </summary>
        public long SizeBytes { get; set; } = 0;
        
        /// <summary>
        /// Hash MD5 du fichier pour vérification d'intégrité
        /// </summary>
        public string Md5Hash { get; set; } = string.Empty;
        
        /// <summary>
        /// Hash SHA-256 du fichier pour vérification d'intégrité
        /// </summary>
        public string Sha256Hash { get; set; } = string.Empty;
        
        /// <summary>
        /// URL de téléchargement du fichier
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Chemin de stockage interne dans le système blob
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Date d'ajout du fichier
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Indique si le fichier est le fichier principal de la version
        /// </summary>
        public bool IsPrimary { get; set; } = true;
        
        /// <summary>
        /// Type de fichier mod (archive principale, patch, ressource additionnelle, etc.)
        /// </summary>
        public FileType FileType { get; set; } = FileType.MainArchive;
        
        /// <summary>
        /// Indique si ce fichier est alternatif ou optionnel
        /// </summary>
        public bool IsOptional { get; set; } = false;
        
        /// <summary>
        /// Description du contenu du fichier
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Résultat du scan de sécurité
        /// </summary>
        public SecurityScan SecurityScan { get; set; } = null;
        
        /// <summary>
        /// Indique si le fichier est approuvé pour téléchargement
        /// </summary>
        public bool IsApproved { get; set; } = false;
    }

    /// <summary>
    /// Types de fichier mod
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// Archive principale du mod (ZIP, RAR, 7z)
        /// </summary>
        MainArchive,
        
        /// <summary>
        /// Patch ou mise à jour
        /// </summary>
        Patch,
        
        /// <summary>
        /// Ressources additionnelles (textures, sons, etc.)
        /// </summary>
        AdditionalResources,
        
        /// <summary>
        /// Fichier de configuration
        /// </summary>
        Configuration,
        
        /// <summary>
        /// Documentation
        /// </summary>
        Documentation,
        
        /// <summary>
        /// Fichier source (pour les mods open source)
        /// </summary>
        SourceCode,
        
        /// <summary>
        /// Autre type de fichier
        /// </summary>
        Other
    }
}
