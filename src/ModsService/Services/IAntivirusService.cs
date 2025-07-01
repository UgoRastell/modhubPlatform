using System.Threading.Tasks;

namespace ModsService.Services
{
    /// <summary>
    /// Interface pour le service de scan antivirus ClamAV
    /// </summary>
    public interface IAntivirusService
    {
        /// <summary>
        /// Scanne un fichier pour détecter d'éventuels virus ou malware
        /// </summary>
        /// <param name="filePath">Chemin vers le fichier à scanner</param>
        /// <returns>True si le fichier est sain, False si un virus est détecté</returns>
        Task<bool> ScanFileAsync(string filePath);
        
        /// <summary>
        /// Scanne un stream de données pour détecter d'éventuels virus ou malware
        /// </summary>
        /// <param name="fileStream">Stream du fichier à scanner</param>
        /// <param name="fileName">Nom du fichier (pour les logs)</param>
        /// <returns>True si le contenu est sain, False si un virus est détecté</returns>
        Task<bool> ScanStreamAsync(Stream fileStream, string fileName);
        
        /// <summary>
        /// Vérifie si le service antivirus est disponible et opérationnel
        /// </summary>
        /// <returns>True si le service est disponible</returns>
        Task<bool> IsServiceAvailableAsync();
    }
}
