using ModsService.Models;
using System.Threading.Tasks;

namespace ModsService.Services.Versioning
{
    /// <summary>
    /// Interface pour le service de gestion de l'historique des versions
    /// </summary>
    public interface IVersionHistoryService
    {
        /// <summary>
        /// Met à jour l'historique des versions d'un mod
        /// </summary>
        /// <param name="mod">Le mod à mettre à jour</param>
        /// <returns>Le mod avec l'historique des versions mis à jour</returns>
        Task<Mod> UpdateVersionHistoryAsync(Mod mod);
        
        /// <summary>
        /// Obtient l'historique des versions formaté
        /// </summary>
        /// <param name="mod">Le mod</param>
        /// <param name="format">Format de sortie (markdown, html, json)</param>
        /// <returns>L'historique des versions au format demandé</returns>
        Task<string> GetFormattedVersionHistoryAsync(Mod mod, string format = "markdown");
        
        /// <summary>
        /// Compare deux versions d'un mod
        /// </summary>
        /// <param name="mod">Le mod</param>
        /// <param name="version1">Première version</param>
        /// <param name="version2">Deuxième version</param>
        /// <returns>Le résultat de la comparaison</returns>
        Task<DiffResult> CompareTwoVersionsAsync(Mod mod, string version1, string version2);
    }
}
