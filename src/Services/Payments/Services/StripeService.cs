using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentsService.Models.DTOs;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Stripe.EventUtility;

namespace PaymentsService.Services;

/// <summary>
/// Implémentation du service Stripe pour interagir avec l'API Stripe
/// </summary>
public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Configure Stripe avec la clé API
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    /// <inheritdoc />
    public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(PaymentIntentRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = request.Amount,
                Currency = request.Currency,
                Description = request.Description,
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", request.UserId },
                    { "ProductId", request.ProductId }
                },
                // Active les modes de paiement automatiques
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);
            
            var publishableKey = _configuration["Stripe:PublishableKey"] ?? string.Empty;
            
            return new PaymentIntentResponse
            {
                ClientSecret = paymentIntent.ClientSecret ?? string.Empty,
                PaymentIntentId = paymentIntent.Id,
                PublishableKey = publishableKey,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erreur Stripe lors de la création du PaymentIntent");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ProcessWebhookEventAsync(string json, string signature)
    {
        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                webhookSecret
            );
            
            // Traiter différents types d'événements
            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandleSuccessfulPaymentAsync(paymentIntent);
                    break;
                
                case "payment_intent.payment_failed":
                    var failedPaymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandleFailedPaymentAsync(failedPaymentIntent);
                    break;
                
                // Ajouter d'autres types d'événements au besoin
                default:
                    _logger.LogInformation("Événement Stripe non traité: {EventType}", stripeEvent.Type);
                    break;
            }
            
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erreur Stripe lors du traitement du webhook");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du traitement du webhook Stripe");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PaymentIntent>> GetUserPaymentsAsync(string userId)
    {
        try
        {
            // Dans un environnement réel, vous devriez avoir une table associant
            // les IDs de PaymentIntent aux utilisateurs. Cette implémentation est simplifiée.
            var service = new PaymentIntentService();
            var options = new PaymentIntentListOptions
            {
                Limit = 100 // Limiter le nombre de résultats
            };
            
            var paymentIntents = await service.ListAsync(options);
            
            // Filtrer les PaymentIntents qui ont ce userId dans les métadonnées
            return paymentIntents.Where(pi => 
                pi.Metadata != null && 
                pi.Metadata.TryGetValue("UserId", out var piUserId) && 
                piUserId == userId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erreur Stripe lors de la récupération des paiements de l'utilisateur");
            throw;
        }
    }
    
    private async Task HandleSuccessfulPaymentAsync(PaymentIntent? paymentIntent)
    {
        if (paymentIntent != null)
        {
            _logger.LogInformation("Paiement réussi pour PaymentIntent {PaymentIntentId}", paymentIntent.Id);
            
            // Extraire les métadonnées
            if (paymentIntent.Metadata != null)
            {
                if (paymentIntent.Metadata.TryGetValue("UserId", out var userId) &&
                    paymentIntent.Metadata.TryGetValue("ProductId", out var productId))
                {
                    // Ici, vous pouvez enregistrer dans votre base de données
                    // ou appeler d'autres services comme ceux des mods ou des utilisateurs
                    // pour activer le contenu acheté
                    
                    _logger.LogInformation("Activation de l'achat pour l'utilisateur {UserId}, produit {ProductId}",
                        userId, productId);
                    
                    // TODO: Implémenter l'activation du produit ou service
                    await Task.CompletedTask; // Pour satisfaire l'attente 'await'
                }
                else
                {
                    _logger.LogWarning("Métadonnées manquantes pour le PaymentIntent {PaymentIntentId}", paymentIntent.Id);
                    await Task.CompletedTask; // Pour satisfaire l'attente 'await'
                }
            }
        }
    }
    
    private async Task HandleFailedPaymentAsync(PaymentIntent? paymentIntent)
    {
        if (paymentIntent != null)
        {
            _logger.LogWarning("Échec du paiement pour PaymentIntent {PaymentIntentId}: {LastError}",
                paymentIntent.Id, paymentIntent.LastPaymentError?.Message ?? "Erreur inconnue");
            
            // Vous pourriez envoyer une notification à l'utilisateur ou effectuer d'autres actions
            await Task.CompletedTask; // Pour satisfaire l'attente 'await'
        }
    }
}
