using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaymentsService.Models.Subscription;
using PaymentsService.Services.Subscription;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionTierController : ControllerBase
    {
        private readonly ISubscriptionTierService _subscriptionTierService;
        private readonly ILogger<SubscriptionTierController> _logger;

        public SubscriptionTierController(ISubscriptionTierService subscriptionTierService, ILogger<SubscriptionTierController> logger)
        {
            _subscriptionTierService = subscriptionTierService ?? throw new ArgumentNullException(nameof(subscriptionTierService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Gestion des niveaux d'abonnement

        [HttpGet]
        public async Task<ActionResult<List<SubscriptionTier>>> GetAllTiers()
        {
            try
            {
                var tiers = await _subscriptionTierService.GetAllTiersAsync();
                return Ok(tiers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des niveaux d'abonnement");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des niveaux d'abonnement");
            }
        }

        [HttpGet("{tierId}")]
        public async Task<ActionResult<SubscriptionTier>> GetTierById(string tierId)
        {
            try
            {
                var tier = await _subscriptionTierService.GetTierByIdAsync(tierId);
                if (tier == null)
                {
                    return NotFound($"Niveau d'abonnement avec l'ID {tierId} non trouvé");
                }
                return Ok(tier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du niveau d'abonnement {TierId}", tierId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du niveau d'abonnement");
            }
        }

        [HttpGet("external/{externalId}")]
        public async Task<ActionResult<SubscriptionTier>> GetTierByExternalId(string externalId)
        {
            try
            {
                var tier = await _subscriptionTierService.GetTierByExternalIdAsync(externalId);
                if (tier == null)
                {
                    return NotFound($"Niveau d'abonnement avec l'ID externe {externalId} non trouvé");
                }
                return Ok(tier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du niveau d'abonnement avec l'ID externe {ExternalId}", externalId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du niveau d'abonnement");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubscriptionTier>> CreateTier([FromBody] SubscriptionTier tier)
        {
            try
            {
                var createdTier = await _subscriptionTierService.CreateTierAsync(tier);
                return CreatedAtAction(nameof(GetTierById), new { tierId = createdTier.Id }, createdTier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du niveau d'abonnement");
                return StatusCode(500, "Une erreur est survenue lors de la création du niveau d'abonnement");
            }
        }

        [HttpPut("{tierId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateTier(string tierId, [FromBody] SubscriptionTier tier)
        {
            try
            {
                var success = await _subscriptionTierService.UpdateTierAsync(tierId, tier);
                if (!success)
                {
                    return NotFound($"Niveau d'abonnement avec l'ID {tierId} non trouvé");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du niveau d'abonnement {TierId}", tierId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du niveau d'abonnement");
            }
        }

        [HttpDelete("{tierId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTier(string tierId)
        {
            try
            {
                var success = await _subscriptionTierService.DeleteTierAsync(tierId);
                if (!success)
                {
                    return NotFound($"Niveau d'abonnement avec l'ID {tierId} non trouvé");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du niveau d'abonnement {TierId}", tierId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du niveau d'abonnement");
            }
        }

        [HttpPatch("{tierId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ToggleTierStatus(string tierId, [FromBody] ToggleTierStatusRequest request)
        {
            try
            {
                var success = await _subscriptionTierService.ToggleTierActiveStatusAsync(tierId, request.IsActive);
                if (!success)
                {
                    return NotFound($"Niveau d'abonnement avec l'ID {tierId} non trouvé");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du statut du niveau d'abonnement {TierId}", tierId);
                return StatusCode(500, "Une erreur est survenue lors de la modification du statut du niveau d'abonnement");
            }
        }

        #endregion

        #region Gestion des avantages

        [HttpPost("{tierId}/benefits")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AddBenefitToTier(string tierId, [FromBody] SubscriptionBenefit benefit)
        {
            try
            {
                var success = await _subscriptionTierService.AddBenefitToTierAsync(tierId, benefit);
                if (!success)
                {
                    return NotFound($"Niveau d'abonnement avec l'ID {tierId} non trouvé");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout d'un avantage au niveau d'abonnement {TierId}", tierId);
                return StatusCode(500, "Une erreur est survenue lors de l'ajout d'un avantage au niveau d'abonnement");
            }
        }

        [HttpDelete("{tierId}/benefits/{benefitName}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RemoveBenefitFromTier(string tierId, string benefitName)
        {
            try
            {
                var success = await _subscriptionTierService.RemoveBenefitFromTierAsync(tierId, benefitName);
                if (!success)
                {
                    return NotFound($"Niveau d'abonnement avec l'ID {tierId} ou avantage {benefitName} non trouvé");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression d'un avantage du niveau d'abonnement {TierId}", tierId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression d'un avantage du niveau d'abonnement");
            }
        }

        #endregion

        #region Gestion des prix

        [HttpGet("{tierId}/price-history")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<SubscriptionTierHistory>>> GetTierPriceHistory(string tierId)
        {
            try
            {
                var history = await _subscriptionTierService.GetTierPriceHistoryAsync(tierId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'historique des prix du niveau d'abonnement {TierId}", tierId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'historique des prix");
            }
        }

        [HttpPatch("{tierId}/price")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateTierPrice(string tierId, [FromBody] UpdateTierPriceRequest request)
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var success = await _subscriptionTierService.UpdateTierPriceAsync(
                    tierId,
                    request.MonthlyPrice,
                    request.YearlyPrice,
                    userId,
                    request.ChangeNotes);

                if (!success)
                {
                    return NotFound($"Niveau d'abonnement avec l'ID {tierId} non trouvé");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du prix du niveau d'abonnement {TierId}", tierId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du prix");
            }
        }

        #endregion

        #region Comparaison et statistiques

        [HttpPost("compare")]
        public async Task<ActionResult<Dictionary<string, List<TierComparisonItem>>>> CompareTiers([FromBody] CompareTiersRequest request)
        {
            try
            {
                var comparison = await _subscriptionTierService.CompareTiersAsync(request.TierIds);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la comparaison des niveaux d'abonnement");
                return StatusCode(500, "Une erreur est survenue lors de la comparaison des niveaux d'abonnement");
            }
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<TierStatistics>>> GetTierStatistics()
        {
            try
            {
                var statistics = await _subscriptionTierService.GetTierStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des niveaux d'abonnement");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        #endregion

        #region Gestion des accès

        [HttpGet("user-features")]
        [Authorize]
        public async Task<ActionResult<List<string>>> GetUserAccessibleFeatures()
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var features = await _subscriptionTierService.GetUserAccessibleFeaturesAsync(userId);
                return Ok(features);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des fonctionnalités accessibles à l'utilisateur");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des fonctionnalités");
            }
        }

        [HttpGet("check-feature-access")]
        [Authorize]
        public async Task<ActionResult<bool>> CheckUserAccessToFeature([FromQuery] string featureName)
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var hasAccess = await _subscriptionTierService.CheckUserAccessToFeatureAsync(userId, featureName);
                return Ok(new { HasAccess = hasAccess });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de l'accès à la fonctionnalité {FeatureName}", featureName);
                return StatusCode(500, "Une erreur est survenue lors de la vérification de l'accès");
            }
        }

        #endregion
    }

    #region DTOs

    /// <summary>
    /// Requête pour activer ou désactiver un niveau d'abonnement
    /// </summary>
    public class ToggleTierStatusRequest
    {
        /// <summary>
        /// Si le niveau doit être actif
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Requête pour mettre à jour le prix d'un niveau d'abonnement
    /// </summary>
    public class UpdateTierPriceRequest
    {
        /// <summary>
        /// Nouveau prix mensuel
        /// </summary>
        public decimal? MonthlyPrice { get; set; }

        /// <summary>
        /// Nouveau prix annuel
        /// </summary>
        public decimal? YearlyPrice { get; set; }

        /// <summary>
        /// Note explicative sur la modification
        /// </summary>
        public string ChangeNotes { get; set; } = string.Empty;
    }

    /// <summary>
    /// Requête pour comparer des niveaux d'abonnement
    /// </summary>
    public class CompareTiersRequest
    {
        /// <summary>
        /// Liste des IDs des niveaux à comparer
        /// </summary>
        public List<string> TierIds { get; set; } = new List<string>();
    }

    #endregion
}
