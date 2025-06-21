using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaymentsService.Models.DTOs;
using Stripe;

namespace PaymentsService.Services
{
    /// <summary>
    /// Interface pour la gestion des abonnements Stripe
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Crée un nouvel abonnement pour un utilisateur
        /// </summary>
        /// <param name="request">Les informations de l'abonnement</param>
        /// <returns>La réponse contenant les informations de l'abonnement</returns>
        Task<SubscriptionResponse> CreateSubscriptionAsync(PaymentIntentRequest request);
        
        /// <summary>
        /// Récupère un abonnement existant
        /// </summary>
        /// <param name="subscriptionId">L'identifiant de l'abonnement</param>
        /// <returns>Les informations de l'abonnement</returns>
        Task<Subscription> GetSubscriptionAsync(string subscriptionId);
        
        /// <summary>
        /// Récupère tous les abonnements d'un utilisateur
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur</param>
        /// <returns>Liste des abonnements de l'utilisateur</returns>
        Task<List<Subscription>> GetUserSubscriptionsAsync(string userId);
        
        /// <summary>
        /// Annule un abonnement
        /// </summary>
        /// <param name="subscriptionId">L'identifiant de l'abonnement</param>
        /// <param name="cancelImmediately">True pour annuler immédiatement, False pour annuler à la fin de la période</param>
        /// <returns>L'abonnement mis à jour</returns>
        Task<Subscription> CancelSubscriptionAsync(string subscriptionId, bool cancelImmediately = false);
        
        /// <summary>
        /// Met à jour un abonnement (changement de plan, quantité, etc.)
        /// </summary>
        /// <param name="subscriptionId">L'identifiant de l'abonnement</param>
        /// <param name="updateRequest">Les informations mises à jour</param>
        /// <returns>L'abonnement mis à jour</returns>
        Task<Subscription> UpdateSubscriptionAsync(string subscriptionId, SubscriptionUpdateRequest updateRequest);
        
        /// <summary>
        /// Traite les événements webhook liés aux abonnements
        /// </summary>
        /// <param name="stripeEvent">L'événement Stripe</param>
        /// <returns>True si l'événement a été traité avec succès</returns>
        Task<bool> ProcessSubscriptionEventAsync(Event stripeEvent);
    }
}
