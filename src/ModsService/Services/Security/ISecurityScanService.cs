using ModsService.Models;
using System.IO;
using System.Threading.Tasks;

namespace ModsService.Services.Security
{
    /// <summary>
    /// Interface pour le service de scan de sécurité des fichiers
    /// </summary>
    public interface ISecurityScanService
    {
        /// <summary>
        /// Effectue un scan de sécurité sur un fichier
        /// </summary>
        /// <param name="fileStream">Le flux du fichier à scanner</param>
        /// <param name="fileName">Le nom du fichier</param>
        /// <param name="contentType">Le type MIME du fichier</param>
        /// <returns>Le résultat du scan de sécurité</returns>
        Task<SecurityScan> ScanFileAsync(Stream fileStream, string fileName, string contentType);
        
        /// <summary>
        /// Effectue un scan de sécurité sur un fichier déjà stocké
        /// </summary>
        /// <param name="filePath">Chemin du fichier dans le stockage</param>
        /// <param name="containerName">Nom du conteneur</param>
        /// <returns>Le résultat du scan de sécurité</returns>
        Task<SecurityScan> ScanStoredFileAsync(string filePath, string containerName);
        
        /// <summary>
        /// Vérifie si un scan est nécessaire pour un fichier donné
        /// </summary>
        /// <param name="modFile">Le fichier mod à vérifier</param>
        /// <returns>True si un scan est nécessaire, false sinon</returns>
        Task<bool> IsScanRequiredAsync(ModFile modFile);
        
        /// <summary>
        /// Vérifie si un fichier est sûr après scan (absence de menaces ou menaces de faible priorité)
        /// </summary>
        /// <param name="scan">Le résultat du scan de sécurité</param>
        /// <returns>True si le fichier est considéré comme sûr, false sinon</returns>
        bool IsFileSafe(SecurityScan scan);
    }
}
