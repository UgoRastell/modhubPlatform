using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PaymentsService.Models;
using PaymentsService.Models.DTOs;
using Stripe;

namespace PaymentsService.Services
{
    /// <summary>
    /// Service pour la traçabilité des transactions de bout en bout
    /// </summary>
    public class TransactionTracingService
    {
        private readonly ILogger<TransactionTracingService> _logger;
        private readonly ITransactionLoggingService _loggingService;
        private readonly IEmailService _emailService;
        private readonly string _environment;

        /// <summary>
        /// Constructeur du service de traçabilité
        /// </summary>
        public TransactionTracingService(
            ILogger<TransactionTracingService> logger,
            ITransactionLoggingService loggingService,
            IEmailService emailService,
            string environment = "development")
        {
            _logger = logger;
            _loggingService = loggingService;
            _emailService = emailService;
            _environment = environment;
        }

        /// <summary>
        /// Traite un événement de création de paiement
        /// </summary>
        /// <param name="request">Requête de création de paiement</param>
        /// <param name="paymentIntent">L'intention de paiement créée</param>
        /// <returns>L'ID de la transaction enregistrée</returns>
        public async Task<string> TracePaymentCreationAsync(PaymentIntentRequest request, PaymentIntent paymentIntent)
        {
            try
            {
                _logger.LogInformation("Traçage de la création d'un paiement pour l'utilisateur {UserId}, montant: {Amount}", 
                    request.UserId, request.Amount);
                
                // Créer une nouvelle transaction
                var transaction = new PaymentTransaction
                {
                    ExternalId = paymentIntent.Id,
                    Type = "payment_intent",
                    UserId = request.UserId,
                    ProductId = request.ProductId,
                    Description = request.Description,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Status = paymentIntent.Status,
                    IsRecurring = request.IsRecurring,
                    RecurringFrequency = request.IsRecurring && request.Frequency.HasValue 
                        ? request.Frequency.Value.ToString() 
                        : string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string>
                    {
                        { "client_secret", paymentIntent.ClientSecret },
                        { "environment", _environment }
                    }
                };
                
                // Journaliser la transaction
                var transactionId = await _loggingService.LogTransactionAsync(transaction);
                
                _logger.LogInformation("Traçage de la création du paiement terminé. TransactionId: {TransactionId}", transactionId);
                
                return transactionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traçage de la création d'un paiement: {Message}", ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// Traite un événement de confirmation de paiement
        /// </summary>
        /// <param name="paymentIntent">L'intention de paiement confirmée</param>
        /// <returns>True si le traçage a réussi</returns>
        public async Task<bool> TracePaymentConfirmationAsync(PaymentIntent paymentIntent)
        {
            try
            {
                _logger.LogInformation("Traçage de la confirmation du paiement {PaymentIntentId}", paymentIntent.Id);
                
                // Chercher la transaction associée à ce paiement
                var transaction = await _loggingService.GetTransactionAsync(paymentIntent.Id);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction non trouvée pour l'intention de paiement {PaymentIntentId}", paymentIntent.Id);
                    return false;
                }
                
                // Mettre à jour le statut
                await _loggingService.UpdateTransactionStatusAsync(transaction.Id, paymentIntent.Status);
                
                // Journaliser l'événement
                var eventDescription = $"Paiement confirmé: {transaction.Amount / 100.0} {transaction.Currency}";
                await _loggingService.LogTransactionEventAsync(transaction.Id, "payment_confirmed", eventDescription);
                
                _logger.LogInformation("Traçage de la confirmation du paiement terminé. TransactionId: {TransactionId}", transaction.Id);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traçage de la confirmation d'un paiement: {Message}", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Traite un événement d'échec de paiement
        /// </summary>
        /// <param name="paymentIntent">L'intention de paiement échouée</param>
        /// <param name="error">Message d'erreur</param>
        /// <returns>True si le traçage a réussi</returns>
        public async Task<bool> TracePaymentFailureAsync(PaymentIntent paymentIntent, string error)
        {
            try
            {
                _logger.LogInformation("Traçage de l'échec du paiement {PaymentIntentId}: {Error}", paymentIntent.Id, error);
                
                // Chercher la transaction associée à ce paiement
                var transaction = await _loggingService.GetTransactionAsync(paymentIntent.Id);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction non trouvée pour l'intention de paiement {PaymentIntentId}", paymentIntent.Id);
                    return false;
                }
                
                // Mettre à jour le statut avec l'erreur
                await _loggingService.UpdateTransactionStatusAsync(transaction.Id, paymentIntent.Status, error);
                
                // Journaliser l'événement
                var eventDescription = $"Échec du paiement: {error}";
                await _loggingService.LogTransactionEventAsync(transaction.Id, "payment_failed", eventDescription);
                
                // Marquer pour révision si nécessaire
                if (transaction.Amount >= 10000) // 100€ ou plus
                {
                    await _loggingService.FlagTransactionForReviewAsync(
                        transaction.Id,
                        $"Échec de paiement d'un montant élevé ({transaction.Amount / 100.0} {transaction.Currency}): {error}",
                        4); // Priorité élevée
                }
                
                _logger.LogInformation("Traçage de l'échec du paiement terminé. TransactionId: {TransactionId}", transaction.Id);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traçage de l'échec d'un paiement: {Message}", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Traite un événement de remboursement
        /// </summary>
        /// <param name="paymentIntentId">L'ID de l'intention de paiement remboursée</param>
        /// <param name="refundId">L'ID du remboursement</param>
        /// <param name="amount">Le montant remboursé</param>
        /// <param name="reason">La raison du remboursement</param>
        /// <returns>True si le traçage a réussi</returns>
        public async Task<bool> TraceRefundAsync(string paymentIntentId, string refundId, long amount, string reason)
        {
            try
            {
                _logger.LogInformation("Traçage du remboursement {RefundId} pour le paiement {PaymentIntentId}", refundId, paymentIntentId);
                
                // Chercher la transaction associée à ce paiement
                var transaction = await _loggingService.GetTransactionAsync(paymentIntentId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction non trouvée pour l'intention de paiement {PaymentIntentId}", paymentIntentId);
                    return false;
                }
                
                // Mettre à jour le statut
                await _loggingService.UpdateTransactionStatusAsync(transaction.Id, "refunded");
                
                // Journaliser l'événement
                var eventDescription = $"Remboursement: {amount / 100.0} {transaction.Currency}, Raison: {reason}";
                await _loggingService.LogTransactionEventAsync(
                    transaction.Id, 
                    "refunded", 
                    eventDescription,
                    new Dictionary<string, string>
                    {
                        { "refund_id", refundId },
                        { "refund_amount", amount.ToString() },
                        { "refund_reason", reason }
                    });
                
                _logger.LogInformation("Traçage du remboursement terminé. TransactionId: {TransactionId}", transaction.Id);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traçage d'un remboursement: {Message}", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Traite un événement de dispute (contestation) de paiement
        /// </summary>
        /// <param name="paymentIntentId">L'ID de l'intention de paiement contestée</param>
        /// <param name="disputeId">L'ID de la contestation</param>
        /// <param name="reason">La raison de la contestation</param>
        /// <returns>True si le traçage a réussi</returns>
        public async Task<bool> TraceDisputeAsync(string paymentIntentId, string disputeId, string reason)
        {
            try
            {
                _logger.LogInformation("Traçage de la contestation {DisputeId} pour le paiement {PaymentIntentId}", disputeId, paymentIntentId);
                
                // Chercher la transaction associée à ce paiement
                var transaction = await _loggingService.GetTransactionAsync(paymentIntentId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction non trouvée pour l'intention de paiement {PaymentIntentId}", paymentIntentId);
                    return false;
                }
                
                // Mettre à jour le statut
                await _loggingService.UpdateTransactionStatusAsync(transaction.Id, "disputed");
                
                // Journaliser l'événement
                var eventDescription = $"Contestation du paiement: {reason}";
                await _loggingService.LogTransactionEventAsync(
                    transaction.Id, 
                    "disputed", 
                    eventDescription,
                    new Dictionary<string, string>
                    {
                        { "dispute_id", disputeId },
                        { "dispute_reason", reason }
                    });
                
                // Marquer pour révision (priorité maximale)
                await _loggingService.FlagTransactionForReviewAsync(
                    transaction.Id,
                    $"Contestation de paiement: {reason}",
                    5); // Priorité maximale
                
                // Alerter les administrateurs
                await _emailService.SendAdminAlertAsync(
                    "Contestation de paiement détectée",
                    $"Une contestation de paiement a été détectée pour la transaction {transaction.Id}.\n\n" +
                    $"Montant: {transaction.Amount / 100.0} {transaction.Currency}\n" +
                    $"Utilisateur: {transaction.UserId}\n" +
                    $"Raison de la contestation: {reason}\n\n" +
                    "Cette situation nécessite une attention immédiate.",
                    5); // Priorité maximale
                
                _logger.LogInformation("Traçage de la contestation terminé. TransactionId: {TransactionId}", transaction.Id);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traçage d'une contestation: {Message}", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Traite un événement d'abonnement
        /// </summary>
        /// <param name="subscription">L'abonnement concerné</param>
        /// <param name="eventType">Le type d'événement (created, updated, deleted, etc.)</param>
        /// <returns>True si le traçage a réussi</returns>
        public async Task<bool> TraceSubscriptionEventAsync(Subscription subscription, string eventType)
        {
            try
            {
                _logger.LogInformation("Traçage de l'événement {EventType} pour l'abonnement {SubscriptionId}", eventType, subscription.Id);
                
                // Extraire l'ID utilisateur des métadonnées
                if (!subscription.Metadata.TryGetValue("user_id", out var userId) || string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Impossible de tracer l'événement d'abonnement: user_id manquant dans les métadonnées");
                    return false;
                }
                
                // Extraire l'ID produit des métadonnées
                if (!subscription.Metadata.TryGetValue("product_id", out var productId))
                {
                    productId = "unknown";
                }
                
                // Récupérer ou créer une transaction pour cet abonnement
                PaymentTransaction transaction;
                transaction = await _loggingService.GetTransactionAsync(subscription.Id);
                
                if (transaction == null && eventType == "subscription.created")
                {
                    // Créer une nouvelle transaction pour cet abonnement
                    transaction = new PaymentTransaction
                    {
                        ExternalId = subscription.Id,
                        Type = "subscription",
                        UserId = userId,
                        ProductId = productId,
                        Description = $"Abonnement: {productId}",
                        // Pour un abonnement, nous prenons le prix total annualisé si possible
                        Amount = GetSubscriptionAmount(subscription),
                        Currency = GetSubscriptionCurrency(subscription),
                        Status = subscription.Status,
                        IsRecurring = true,
                        RecurringFrequency = subscription.Items?.Data[0]?.Plan?.Interval ?? "unknown",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Metadata = new Dictionary<string, string>
                        {
                            { "subscription_id", subscription.Id },
                            { "environment", _environment }
                        }
                    };
                    
                    // Journaliser la transaction
                    await _loggingService.LogTransactionAsync(transaction);
                }
                else if (transaction != null)
                {
                    // Mettre à jour le statut de la transaction existante
                    await _loggingService.UpdateTransactionStatusAsync(transaction.Id, subscription.Status);
                    
                    // Journaliser l'événement
                    var description = $"Événement d'abonnement: {eventType}";
                    await _loggingService.LogTransactionEventAsync(
                        transaction.Id,
                        eventType,
                        description,
                        new Dictionary<string, string>
                        {
                            { "subscription_id", subscription.Id },
                            { "status", subscription.Status }
                        });
                }
                else
                {
                    _logger.LogWarning("Transaction non trouvée pour l'abonnement {SubscriptionId} et ce n'est pas un événement de création", subscription.Id);
                    return false;
                }
                
                _logger.LogInformation("Traçage de l'événement d'abonnement terminé. SubscriptionId: {SubscriptionId}", subscription.Id);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traçage d'un événement d'abonnement: {Message}", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Récupère le montant total d'un abonnement
        /// </summary>
        private long GetSubscriptionAmount(Subscription subscription)
        {
            if (subscription.Items?.Data == null || subscription.Items.Data.Count == 0)
                return 0;
                
            var item = subscription.Items.Data[0];
            var plan = item.Plan;
            
            if (plan == null)
                return 0;
                
            // Calculer un montant annualisé pour faciliter les comparaisons
            switch (plan.Interval?.ToLower())
            {
                case "day":
                    return (plan.Amount ?? 0) * 365 * (item.Quantity == 0 ? 1L : (long)item.Quantity);
                case "week":
                    return (plan.Amount ?? 0) * 52 * (item.Quantity == 0 ? 1L : (long)item.Quantity);
                case "month":
                    return (plan.Amount ?? 0) * 12 * (item.Quantity == 0 ? 1L : (long)item.Quantity);
                case "year":
                    return (plan.Amount ?? 0) * (item.Quantity == 0 ? 1L : (long)item.Quantity);
                default:
                    return (plan.Amount ?? 0) * (item.Quantity == 0 ? 1L : (long)item.Quantity);
            }
        }
        
        /// <summary>
        /// Récupère la devise d'un abonnement
        /// </summary>
        private string GetSubscriptionCurrency(Subscription subscription)
        {
            if (subscription.Items?.Data == null || subscription.Items.Data.Count == 0)
                return "eur";
                
            var plan = subscription.Items.Data[0].Plan;
            return plan?.Currency?.ToLower() ?? "eur";
        }
    }
}
