namespace ModsService.Services.Storage
{
    /// <summary>
    /// Configuration pour le service de stockage blob
    /// </summary>
    public class BlobStorageSettings
    {
        /// <summary>
        /// Type de stockage à utiliser (Local, AzureBlobStorage, etc.)
        /// </summary>
        public string StorageType { get; set; } = "Local";
        
        /// <summary>
        /// Chemin racine pour le stockage local des fichiers
        /// </summary>
        public string LocalStoragePath { get; set; } = "FileStorage";
        
        /// <summary>
        /// Chaîne de connexion pour Azure Blob Storage
        /// </summary>
        public string AzureBlobConnectionString { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du compte de stockage Azure
        /// </summary>
        public string AzureAccountName { get; set; } = string.Empty;
        
        /// <summary>
        /// URL de base pour accéder aux fichiers
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:5000/files";
        
        /// <summary>
        /// Durée de validité par défaut des URLs signées (en minutes)
        /// </summary>
        public int DefaultUrlExpiryMinutes { get; set; } = 60;
        
        /// <summary>
        /// Taille maximale de fichier autorisée en Mo
        /// </summary>
        public long MaxFileSizeMB { get; set; } = 500;
        
        /// <summary>
        /// Extensions de fichiers autorisées, séparées par des virgules
        /// </summary>
        public string AllowedExtensions { get; set; } = ".zip,.rar,.7z";
        
        /// <summary>
        /// Indique si le scan antivirus est activé
        /// </summary>
        public bool EnableVirusScan { get; set; } = true;
        
        /// <summary>
        /// URL de l'API de scan antivirus (si externe)
        /// </summary>
        public string VirusScanApiUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Clé d'API pour le service de scan antivirus
        /// </summary>
        public string VirusScanApiKey { get; set; } = string.Empty;
    }
}
