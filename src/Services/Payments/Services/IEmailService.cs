using System;
using System.Threading.Tasks;

namespace PaymentsService.Services
{
    /// <summary>
    /// Interface pour le service d'envoi d'e-mails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Envoie une notification par e-mail liée à une transaction à un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur destinataire</param>
        /// <param name="subject">Sujet de l'e-mail</param>
        /// <param name="message">Corps du message</param>
        /// <returns>True si l'envoi a réussi</returns>
        Task<bool> SendTransactionNotificationAsync(string userId, string subject, string message);
        
        /// <summary>
        /// Envoie une alerte aux administrateurs concernant une transaction
        /// </summary>
        /// <param name="subject">Sujet de l'alerte</param>
        /// <param name="message">Corps du message</param>
        /// <param name="priority">Niveau de priorité (1-5)</param>
        /// <returns>True si l'envoi a réussi</returns>
        Task<bool> SendAdminAlertAsync(string subject, string message, int priority = 3);
        
        /// <summary>
        /// Envoie un récapitulatif des transactions à un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur destinataire</param>
        /// <param name="startDate">Date de début de la période</param>
        /// <param name="endDate">Date de fin de la période</param>
        /// <returns>True si l'envoi a réussi</returns>
        Task<bool> SendTransactionSummaryAsync(string userId, DateTime startDate, DateTime endDate);
    }
}
