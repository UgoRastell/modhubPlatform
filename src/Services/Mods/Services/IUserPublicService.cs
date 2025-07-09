using System.Threading.Tasks;
using ModsService.Models.DTOs;

namespace ModsService.Services
{
    /// <summary>
    /// Service chargé de récupérer les informations publiques d'un utilisateur à partir du UsersService.
    /// </summary>
    public interface IUserPublicService
    {
        /// <summary>
        /// Récupère de façon asynchrone le profil public minimal d'un utilisateur.
        /// </summary>
        /// <param name="userId">Id de l'utilisateur.</param>
        /// <returns>Instance de <see cref="PublicUserDto"/> ou null si l'utilisateur n'est pas trouvé.</returns>
        Task<PublicUserDto?> GetPublicUserAsync(string userId);
    }
}
