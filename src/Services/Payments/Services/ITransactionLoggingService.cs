using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaymentsService.Models;

namespace PaymentsService.Services
{
    /// <summary>
    /// Interface pour le service de journalisation des transactions
    /// </summary>
    public interface ITransactionLoggingService
    {
        /// <summary>
        /// Enregistre une nouvelle transaction de paiement
        /// </summary>
        /// <param name="transaction">Détails de la transaction</param>
        /// <returns>L'identifiant de la transaction enregistrée</returns>
        Task<string> LogTransactionAsync(PaymentTransaction transaction);
        
        /// <summary>
        /// Met à jour le statut d'une transaction existante
        /// </summary>
        /// <param name="transactionId">Identifiant de la transaction</param>
        /// <param name="newStatus">Nouveau statut</param>
        /// <param name="errorMessage">Message d'erreur optionnel</param>
        /// <returns>True si la mise à jour a réussi</returns>
        Task<bool> UpdateTransactionStatusAsync(string transactionId, string newStatus, string? errorMessage = null);
        
        /// <summary>
        /// Récupère une transaction par son identifiant
        /// </summary>
        /// <param name="transactionId">Identifiant de la transaction</param>
        /// <returns>Détails de la transaction</returns>
        Task<PaymentTransaction> GetTransactionAsync(string transactionId);
        
        /// <summary>
        /// Récupère toutes les transactions d'un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <param name="includeDetails">Si true, inclut les détails complets de chaque transaction</param>
        /// <returns>Liste des transactions</returns>
        Task<List<PaymentTransaction>> GetUserTransactionsAsync(string userId, bool includeDetails = false);
        
        /// <summary>
        /// Journalise un événement lié à une transaction
        /// </summary>
        /// <param name="transactionId">Identifiant de la transaction</param>
        /// <param name="eventType">Type d'événement</param>
        /// <param name="description">Description de l'événement</param>
        /// <param name="metadata">Métadonnées additionnelles</param>
        /// <returns>True si l'événement a été journalisé avec succès</returns>
        Task<bool> LogTransactionEventAsync(string transactionId, string eventType, string description, Dictionary<string, string>? metadata = null);
        
        /// <summary>
        /// Marque une transaction comme nécessitant une intervention manuelle
        /// </summary>
        /// <param name="transactionId">Identifiant de la transaction</param>
        /// <param name="reason">Raison de l'intervention</param>
        /// <param name="priority">Niveau de priorité (1-5)</param>
        /// <returns>True si la transaction a été marquée avec succès</returns>
        Task<bool> FlagTransactionForReviewAsync(string transactionId, string reason, int priority = 3);
    }
}
