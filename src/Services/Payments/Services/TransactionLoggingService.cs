using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentsService.Config;
using PaymentsService.Models;
using PaymentsService.Repositories;

namespace PaymentsService.Services
{
    /// <summary>
    /// Service de journalisation des transactions de paiement
    /// </summary>
    public class TransactionLoggingService : ITransactionLoggingService
    {
        private readonly ILogger<TransactionLoggingService> _logger;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IEmailService? _emailService;
        
        /// <summary>
        /// Constructeur du service de journalisation des transactions
        /// </summary>
        public TransactionLoggingService(
            ILogger<TransactionLoggingService> logger,
            ITransactionRepository transactionRepository,
            IEmailService? emailService = null)
        {
            _logger = logger;
            _transactionRepository = transactionRepository;
            _emailService = emailService;
        }

        /// <inheritdoc />
        public async Task<string> LogTransactionAsync(PaymentTransaction transaction)
        {
            try
            {
                _logger.LogInformation("Journalisation d'une nouvelle transaction pour l'utilisateur {UserId}, montant: {Amount} {Currency}",
                    transaction.UserId, transaction.Amount / 100.0, transaction.Currency);
                
                // Ajouter l'événement de création à l'historique
                transaction.Events.Add(new TransactionEvent
                {
                    EventType = "created",
                    Description = $"Transaction créée pour {transaction.Amount / 100.0} {transaction.Currency}"
                });
                
                // Sauvegarder la transaction
                var transactionId = await _transactionRepository.CreateTransactionAsync(transaction);
                
                // Envoyer une notification par email en arrière-plan si le service est disponible
                if (_emailService != null)
                {
                    // Ne pas attendre l'envoi de l'e-mail pour ne pas bloquer la réponse
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendTransactionNotificationAsync(
                                transaction.UserId,
                                "Nouvelle transaction enregistrée",
                                $"Votre transaction de {transaction.Amount / 100.0} {transaction.Currency} a été enregistrée."
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erreur lors de l'envoi de l'e-mail de notification pour la transaction {TransactionId}", transactionId);
                        }
                    });
                }
                
                return transactionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la journalisation d'une transaction pour l'utilisateur {UserId}: {Message}",
                    transaction.UserId, ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateTransactionStatusAsync(string transactionId, string newStatus, string? errorMessage = null)
        {
            try
            {
                _logger.LogInformation("Mise à jour du statut de la transaction {TransactionId} vers {NewStatus}",
                    transactionId, newStatus);
                
                // Récupérer la transaction existante
                var transaction = await _transactionRepository.GetTransactionAsync(transactionId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction {TransactionId} non trouvée lors de la mise à jour du statut", transactionId);
                    return false;
                }
                
                // Mettre à jour le statut
                transaction.Status = newStatus;
                transaction.UpdatedAt = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    transaction.ErrorMessage = errorMessage;
                }
                
                // Ajouter l'événement à l'historique
                var description = $"Statut mis à jour vers {newStatus}";
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    description += $" avec erreur: {errorMessage}";
                }
                
                transaction.Events.Add(new TransactionEvent
                {
                    EventType = "status_updated",
                    Description = description
                });
                
                // Sauvegarder les modifications
                await _transactionRepository.UpdateTransactionAsync(transaction);
                
                // Envoyer une notification pour les statuts importants
                if (_emailService != null && 
                    (newStatus == "succeeded" || newStatus == "failed" || newStatus == "refunded"))
                {
                    // Ne pas attendre l'envoi de l'e-mail pour ne pas bloquer la réponse
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string subject = string.Empty;
                            string message = string.Empty;
                            
                            switch (newStatus)
                            {
                                case "succeeded":
                                    subject = "Paiement réussi";
                                    message = $"Votre paiement de {transaction.Amount / 100.0} {transaction.Currency} a été traité avec succès.";
                                    break;
                                case "failed":
                                    subject = "Échec de paiement";
                                    message = $"Votre paiement de {transaction.Amount / 100.0} {transaction.Currency} a échoué. Erreur: {errorMessage}";
                                    break;
                                case "refunded":
                                    subject = "Remboursement effectué";
                                    message = $"Votre paiement de {transaction.Amount / 100.0} {transaction.Currency} a été remboursé.";
                                    break;
                            }
                            
                            await _emailService.SendTransactionNotificationAsync(
                                transaction.UserId,
                                subject,
                                message
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erreur lors de l'envoi de l'e-mail de notification pour la mise à jour de la transaction {TransactionId}", transactionId);
                        }
                    });
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du statut de la transaction {TransactionId}: {Message}",
                    transactionId, ex.Message);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<PaymentTransaction> GetTransactionAsync(string transactionId)
        {
            try
            {
                _logger.LogDebug("Récupération de la transaction {TransactionId}", transactionId);
                return await _transactionRepository.GetTransactionAsync(transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la transaction {TransactionId}: {Message}",
                    transactionId, ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<PaymentTransaction>> GetUserTransactionsAsync(string userId, bool includeDetails = false)
        {
            try
            {
                _logger.LogDebug("Récupération des transactions pour l'utilisateur {UserId}", userId);
                return await _transactionRepository.GetUserTransactionsAsync(userId, includeDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des transactions pour l'utilisateur {UserId}: {Message}",
                    userId, ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> LogTransactionEventAsync(string transactionId, string eventType, string description, Dictionary<string, string>? metadata = null)
        {
            try
            {
                _logger.LogInformation("Journalisation d'un événement {EventType} pour la transaction {TransactionId}",
                    eventType, transactionId);
                
                // Récupérer la transaction existante
                var transaction = await _transactionRepository.GetTransactionAsync(transactionId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction {TransactionId} non trouvée lors de la journalisation d'un événement", transactionId);
                    return false;
                }
                
                // Créer le nouvel événement
                var newEvent = new TransactionEvent
                {
                    EventType = eventType,
                    Description = description,
                    Timestamp = DateTime.UtcNow
                };
                
                if (metadata != null)
                {
                    newEvent.Metadata = new Dictionary<string, string>(metadata);
                }
                
                // Ajouter l'événement à l'historique
                transaction.Events.Add(newEvent);
                transaction.UpdatedAt = DateTime.UtcNow;
                
                // Sauvegarder les modifications
                await _transactionRepository.UpdateTransactionAsync(transaction);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la journalisation d'un événement pour la transaction {TransactionId}: {Message}",
                    transactionId, ex.Message);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> FlagTransactionForReviewAsync(string transactionId, string reason, int priority = 3)
        {
            try
            {
                _logger.LogInformation("Marquage de la transaction {TransactionId} pour vérification manuelle, priorité: {Priority}",
                    transactionId, priority);
                
                // Récupérer la transaction existante
                var transaction = await _transactionRepository.GetTransactionAsync(transactionId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction {TransactionId} non trouvée lors du marquage pour vérification", transactionId);
                    return false;
                }
                
                // Mettre à jour les champs de vérification
                transaction.RequiresReview = true;
                transaction.ReviewReason = reason;
                transaction.ReviewPriority = Math.Clamp(priority, 1, 5); // Assurer que la priorité est entre 1 et 5
                transaction.UpdatedAt = DateTime.UtcNow;
                
                // Ajouter l'événement à l'historique
                transaction.Events.Add(new TransactionEvent
                {
                    EventType = "flagged_for_review",
                    Description = $"Transaction marquée pour vérification: {reason}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "priority", priority.ToString() }
                    }
                });
                
                // Sauvegarder les modifications
                await _transactionRepository.UpdateTransactionAsync(transaction);
                
                // Alerte par email aux administrateurs pour les cas prioritaires
                if (_emailService != null && priority >= 4)
                {
                    // Ne pas attendre l'envoi de l'e-mail pour ne pas bloquer la réponse
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendAdminAlertAsync(
                                "Transaction nécessitant une vérification urgente",
                                $"La transaction {transactionId} a été marquée pour vérification urgente (priorité {priority}/5).\n" +
                                $"Raison: {reason}\n" +
                                $"Utilisateur: {transaction.UserId}\n" +
                                $"Montant: {transaction.Amount / 100.0} {transaction.Currency}"
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erreur lors de l'envoi de l'alerte admin pour la transaction {TransactionId}", transactionId);
                        }
                    });
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage de la transaction {TransactionId} pour vérification: {Message}",
                    transactionId, ex.Message);
                return false;
            }
        }
    }
}
