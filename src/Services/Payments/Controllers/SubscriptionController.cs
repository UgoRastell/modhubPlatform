using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaymentsService.Models.DTOs;
using PaymentsService.Services;
using Stripe;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ILogger<SubscriptionController> _logger;
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(
            ILogger<SubscriptionController> logger,
            ISubscriptionService subscriptionService)
        {
            _logger = logger;
            _subscriptionService = subscriptionService;
        }

        /// <summary>
        /// Crée un nouvel abonnement pour un utilisateur
        /// </summary>
        /// <param name="request">Les informations de l'abonnement</param>
        /// <returns>La réponse contenant les informations de l'abonnement</returns>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SubscriptionResponse>> CreateSubscription([FromBody] PaymentIntentRequest request)
        {
            try
            {
                // Vérifier que la requête concerne un abonnement
                if (!request.IsRecurring || !request.Frequency.HasValue)
                {
                    return BadRequest("La requête doit être récurrente et avoir une fréquence définie pour créer un abonnement.");
                }

                var response = await _subscriptionService.CreateSubscriptionAsync(request);
                return response;
            }
            catch (ArgumentException ae)
            {
                _logger.LogWarning(ae, "Requête invalide pour la création d'un abonnement: {Message}", ae.Message);
                return BadRequest(ae.Message);
            }
            catch (StripeException se)
            {
                _logger.LogError(se, "Erreur Stripe lors de la création d'un abonnement: {Message}", se.Message);
                return StatusCode(500, $"Erreur lors de la création de l'abonnement: {se.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'un abonnement: {Message}", ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la création de l'abonnement.");
            }
        }

        /// <summary>
        /// Récupère un abonnement par son identifiant
        /// </summary>
        /// <param name="id">Identifiant de l'abonnement</param>
        /// <returns>Les détails de l'abonnement</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Subscription>> GetSubscription(string id)
        {
            try
            {
                var subscription = await _subscriptionService.GetSubscriptionAsync(id);
                if (subscription == null)
                {
                    return NotFound($"Abonnement avec l'identifiant {id} non trouvé.");
                }
                return subscription;
            }
            catch (StripeException se)
            {
                _logger.LogError(se, "Erreur Stripe lors de la récupération de l'abonnement {SubscriptionId}: {Message}", id, se.Message);
                return StatusCode(500, $"Erreur lors de la récupération de l'abonnement: {se.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'abonnement {SubscriptionId}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'abonnement.");
            }
        }

        /// <summary>
        /// Récupère tous les abonnements d'un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <returns>Liste des abonnements de l'utilisateur</returns>
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<ActionResult<List<Subscription>>> GetUserSubscriptions(string userId)
        {
            try
            {
                var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId);
                return subscriptions;
            }
            catch (StripeException se)
            {
                _logger.LogError(se, "Erreur Stripe lors de la récupération des abonnements pour l'utilisateur {UserId}: {Message}", userId, se.Message);
                return StatusCode(500, $"Erreur lors de la récupération des abonnements: {se.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des abonnements pour l'utilisateur {UserId}: {Message}", userId, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des abonnements.");
            }
        }

        /// <summary>
        /// Annule un abonnement
        /// </summary>
        /// <param name="id">Identifiant de l'abonnement</param>
        /// <param name="cancelImmediately">Si true, annule immédiatement l'abonnement</param>
        /// <returns>L'abonnement mis à jour</returns>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<Subscription>> CancelSubscription(string id, [FromQuery] bool cancelImmediately = false)
        {
            try
            {
                var subscription = await _subscriptionService.CancelSubscriptionAsync(id, cancelImmediately);
                return subscription;
            }
            catch (StripeException se)
            {
                _logger.LogError(se, "Erreur Stripe lors de l'annulation de l'abonnement {SubscriptionId}: {Message}", id, se.Message);
                return StatusCode(500, $"Erreur lors de l'annulation de l'abonnement: {se.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'annulation de l'abonnement {SubscriptionId}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de l'annulation de l'abonnement.");
            }
        }

        /// <summary>
        /// Met à jour un abonnement
        /// </summary>
        /// <param name="id">Identifiant de l'abonnement</param>
        /// <param name="request">Modifications à appliquer</param>
        /// <returns>L'abonnement mis à jour</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<Subscription>> UpdateSubscription(string id, [FromBody] SubscriptionUpdateRequest request)
        {
            try
            {
                var subscription = await _subscriptionService.UpdateSubscriptionAsync(id, request);
                return subscription;
            }
            catch (StripeException se)
            {
                _logger.LogError(se, "Erreur Stripe lors de la mise à jour de l'abonnement {SubscriptionId}: {Message}", id, se.Message);
                return StatusCode(500, $"Erreur lors de la mise à jour de l'abonnement: {se.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'abonnement {SubscriptionId}: {Message}", id, ex.Message);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'abonnement.");
            }
        }
    }
}
