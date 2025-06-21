using PaymentsService.Models.DTOs;
using Stripe;

namespace PaymentsService.Services;

/// <summary>
/// Service pour interagir avec l'API Stripe
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Crée un PaymentIntent pour initier un paiement
    /// </summary>
    /// <param name="request">Données nécessaires pour créer le PaymentIntent</param>
    /// <returns>Les informations du PaymentIntent créé</returns>
    Task<PaymentIntentResponse> CreatePaymentIntentAsync(PaymentIntentRequest request);
    
    /// <summary>
    /// Traite un événement Stripe reçu via webhook
    /// </summary>
    /// <param name="json">Contenu du webhook</param>
    /// <param name="signature">Signature du webhook pour vérification</param>
    /// <returns>True si l'événement a été traité avec succès, sinon false</returns>
    Task<bool> ProcessWebhookEventAsync(string json, string signature);
    
    /// <summary>
    /// Récupère l'historique des paiements d'un utilisateur
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Liste des paiements de l'utilisateur</returns>
    Task<IEnumerable<PaymentIntent>> GetUserPaymentsAsync(string userId);
}
