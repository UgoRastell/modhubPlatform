using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaymentsService.Models;

namespace PaymentsService.Repositories
{
    /// <summary>
    /// Interface pour le repository de transactions de paiement
    /// </summary>
    public interface ITransactionRepository
    {
        /// <summary>
        /// Crée une nouvelle transaction dans la base de données
        /// </summary>
        /// <param name="transaction">Transaction à créer</param>
        /// <returns>Identifiant de la transaction créée</returns>
        Task<string> CreateTransactionAsync(PaymentTransaction transaction);
        
        /// <summary>
        /// Met à jour une transaction existante
        /// </summary>
        /// <param name="transaction">Transaction avec les modifications</param>
        /// <returns>True si la mise à jour a réussi</returns>
        Task<bool> UpdateTransactionAsync(PaymentTransaction transaction);
        
        /// <summary>
        /// Récupère une transaction par son identifiant
        /// </summary>
        /// <param name="transactionId">Identifiant de la transaction</param>
        /// <returns>Détails de la transaction ou null si non trouvée</returns>
        Task<PaymentTransaction> GetTransactionAsync(string transactionId);
        
        /// <summary>
        /// Récupère une transaction par son identifiant externe (par exemple, Stripe ID)
        /// </summary>
        /// <param name="externalId">Identifiant externe</param>
        /// <returns>Détails de la transaction ou null si non trouvée</returns>
        Task<PaymentTransaction> GetTransactionByExternalIdAsync(string externalId);
        
        /// <summary>
        /// Récupère toutes les transactions d'un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <param name="includeDetails">Si true, inclut les détails complets de chaque transaction</param>
        /// <returns>Liste des transactions</returns>
        Task<List<PaymentTransaction>> GetUserTransactionsAsync(string userId, bool includeDetails = false);
        
        /// <summary>
        /// Récupère les transactions nécessitant une vérification
        /// </summary>
        /// <param name="maxResults">Nombre maximum de résultats</param>
        /// <param name="minPriority">Priorité minimale</param>
        /// <returns>Liste des transactions à vérifier</returns>
        Task<List<PaymentTransaction>> GetTransactionsRequiringReviewAsync(int maxResults = 50, int minPriority = 1);
        
        /// <summary>
        /// Recherche des transactions par différents critères
        /// </summary>
        /// <param name="startDate">Date de début</param>
        /// <param name="endDate">Date de fin</param>
        /// <param name="status">Statut</param>
        /// <param name="minAmount">Montant minimum</param>
        /// <param name="maxAmount">Montant maximum</param>
        /// <param name="isRecurring">Si true, uniquement les abonnements récurrents</param>
        /// <returns>Liste des transactions correspondant aux critères</returns>
        Task<List<PaymentTransaction>> SearchTransactionsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? status = null,
            long? minAmount = null,
            long? maxAmount = null,
            bool? isRecurring = null);
    }
}
