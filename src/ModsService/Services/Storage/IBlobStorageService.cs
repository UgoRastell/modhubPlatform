using Microsoft.AspNetCore.Http;
using ModsService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ModsService.Services.Storage
{
    /// <summary>
    /// Interface définissant les opérations de stockage de fichiers blob
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Téléverse un fichier dans le stockage blob
        /// </summary>
        /// <param name="file">Fichier à téléverser</param>
        /// <param name="containerName">Nom du conteneur (ex: "mods", "avatars", etc.)</param>
        /// <param name="filePath">Chemin personnalisé dans le conteneur (optionnel)</param>
        /// <returns>Informations sur le fichier téléversé</returns>
        Task<ModFile> UploadFileAsync(IFormFile file, string containerName, string filePath = null);
        
        /// <summary>
        /// Téléverse un fichier depuis un flux dans le stockage blob
        /// </summary>
        /// <param name="fileStream">Flux contenant le fichier</param>
        /// <param name="fileName">Nom du fichier</param>
        /// <param name="contentType">Type MIME du fichier</param>
        /// <param name="containerName">Nom du conteneur (ex: "mods", "avatars", etc.)</param>
        /// <param name="filePath">Chemin personnalisé dans le conteneur (optionnel)</param>
        /// <returns>Informations sur le fichier téléversé</returns>
        Task<ModFile> UploadFileAsync(Stream fileStream, string fileName, string contentType, string containerName, string filePath = null);
        
        /// <summary>
        /// Récupère un fichier depuis le stockage blob
        /// </summary>
        /// <param name="filePath">Chemin du fichier dans le stockage</param>
        /// <param name="containerName">Nom du conteneur</param>
        /// <returns>Flux contenant le fichier récupéré</returns>
        Task<Stream> DownloadFileAsync(string filePath, string containerName);
        
        /// <summary>
        /// Récupère l'URL de téléchargement d'un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier dans le stockage</param>
        /// <param name="containerName">Nom du conteneur</param>
        /// <param name="expiryTime">Durée de validité de l'URL (optionnel)</param>
        /// <returns>URL signée pour accéder au fichier</returns>
        Task<string> GetFileUrlAsync(string filePath, string containerName, TimeSpan? expiryTime = null);
        
        /// <summary>
        /// Vérifie si un fichier existe dans le stockage
        /// </summary>
        /// <param name="filePath">Chemin du fichier dans le stockage</param>
        /// <param name="containerName">Nom du conteneur</param>
        /// <returns>True si le fichier existe, false sinon</returns>
        Task<bool> FileExistsAsync(string filePath, string containerName);
        
        /// <summary>
        /// Supprime un fichier du stockage
        /// </summary>
        /// <param name="filePath">Chemin du fichier dans le stockage</param>
        /// <param name="containerName">Nom du conteneur</param>
        /// <returns>True si la suppression a réussi, false sinon</returns>
        Task<bool> DeleteFileAsync(string filePath, string containerName);
        
        /// <summary>
        /// Liste les fichiers dans un conteneur
        /// </summary>
        /// <param name="containerName">Nom du conteneur</param>
        /// <param name="prefix">Préfixe pour filtrer les fichiers (optionnel)</param>
        /// <returns>Liste des chemins de fichiers</returns>
        Task<IEnumerable<string>> ListFilesAsync(string containerName, string prefix = null);
        
        /// <summary>
        /// Obtient les métadonnées d'un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier dans le stockage</param>
        /// <param name="containerName">Nom du conteneur</param>
        /// <returns>Dictionnaire de métadonnées du fichier</returns>
        Task<Dictionary<string, string>> GetFileMetadataAsync(string filePath, string containerName);
        
        /// <summary>
        /// Définit les métadonnées d'un fichier
        /// </summary>
        /// <param name="filePath">Chemin du fichier dans le stockage</param>
        /// <param name="containerName">Nom du conteneur</param>
        /// <param name="metadata">Métadonnées à définir</param>
        Task SetFileMetadataAsync(string filePath, string containerName, Dictionary<string, string> metadata);
    }
}
