using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaymentsService.Models.Affiliate;
using PaymentsService.Services.Affiliate;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AffiliateController : ControllerBase
    {
        private readonly IAffiliateService _affiliateService;
        private readonly ILogger<AffiliateController> _logger;

        public AffiliateController(IAffiliateService affiliateService, ILogger<AffiliateController> logger)
        {
            _affiliateService = affiliateService ?? throw new ArgumentNullException(nameof(affiliateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Programme d'affiliation

        [HttpGet("programs")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<AffiliateProgram>>> GetAllPrograms()
        {
            try
            {
                var programs = await _affiliateService.GetAllProgramsAsync();
                return Ok(programs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des programmes d'affiliation");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des programmes d'affiliation");
            }
        }

        [HttpGet("programs/{programId}")]
        public async Task<ActionResult<AffiliateProgram>> GetProgramById(string programId)
        {
            try
            {
                var program = await _affiliateService.GetProgramByIdAsync(programId);
                if (program == null)
                {
                    return NotFound($"Programme d'affiliation avec l'ID {programId} non trouvé");
                }
                return Ok(program);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du programme d'affiliation {ProgramId}", programId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du programme d'affiliation");
            }
        }

        [HttpPost("programs")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AffiliateProgram>> CreateProgram([FromBody] AffiliateProgram program)
        {
            try
            {
                var createdProgram = await _affiliateService.CreateProgramAsync(program);
                return CreatedAtAction(nameof(GetProgramById), new { programId = createdProgram.Id }, createdProgram);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du programme d'affiliation");
                return StatusCode(500, "Une erreur est survenue lors de la création du programme d'affiliation");
            }
        }

        [HttpPut("programs/{programId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateProgram(string programId, [FromBody] AffiliateProgram program)
        {
            try
            {
                var success = await _affiliateService.UpdateProgramAsync(programId, program);
                if (!success)
                {
                    return NotFound($"Programme d'affiliation avec l'ID {programId} non trouvé");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du programme d'affiliation {ProgramId}", programId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du programme d'affiliation");
            }
        }

        [HttpDelete("programs/{programId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteProgram(string programId)
        {
            try
            {
                var success = await _affiliateService.DeleteProgramAsync(programId);
                if (!success)
                {
                    return NotFound($"Programme d'affiliation avec l'ID {programId} non trouvé");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du programme d'affiliation {ProgramId}", programId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du programme d'affiliation");
            }
        }

        #endregion

        #region Liens d'affiliation

        [HttpPost("links")]
        [Authorize]
        public async Task<ActionResult<AffiliateLink>> GenerateAffiliateLink([FromBody] LinkGenerationRequest request)
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var link = await _affiliateService.GenerateAffiliateLinkAsync(userId, request.TargetType, request.TargetId);
                return CreatedAtAction(nameof(GetAffiliateLink), new { linkId = link.Id }, link);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du lien d'affiliation");
                return StatusCode(500, "Une erreur est survenue lors de la génération du lien d'affiliation");
            }
        }

        [HttpGet("links/{linkId}")]
        public async Task<ActionResult<AffiliateLink>> GetAffiliateLink(string linkId)
        {
            // Cette méthode serait implémentée dans le service, ajoutez-la si nécessaire
            return Ok(new AffiliateLink { Id = linkId });
        }

        [HttpGet("links")]
        [Authorize]
        public async Task<ActionResult<List<AffiliateLink>>> GetUserAffiliateLinks()
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                // Cette méthode serait implémentée dans le service, ajoutez-la si nécessaire
                return Ok(new List<AffiliateLink>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des liens d'affiliation");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des liens d'affiliation");
            }
        }

        #endregion

        #region Clics et conversions

        [HttpPost("track-click")]
        [AllowAnonymous]
        public async Task<ActionResult> TrackClick([FromBody] ClickTrackingRequest request)
        {
            try
            {
                // Récupérer l'adresse IP et l'agent utilisateur
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var userAgent = Request.Headers["User-Agent"].ToString();

                var click = await _affiliateService.TrackAffiliateLinkClickAsync(request.LinkId, ipAddress, userAgent);
                return Ok(new { Success = true, ClickId = click.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement du clic sur le lien d'affiliation");
                return StatusCode(500, "Une erreur est survenue lors de l'enregistrement du clic");
            }
        }

        [HttpPost("track-conversion")]
        [Authorize]
        public async Task<ActionResult> TrackConversion([FromBody] ConversionTrackingRequest request)
        {
            try
            {
                var conversion = await _affiliateService.TrackConversionAsync(request.ClickId, request.Amount, request.OrderId);
                return Ok(new { Success = true, ConversionId = conversion.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement de la conversion d'affiliation");
                return StatusCode(500, "Une erreur est survenue lors de l'enregistrement de la conversion");
            }
        }

        #endregion

        #region Commissions et paiements

        [HttpGet("commissions")]
        [Authorize]
        public async Task<ActionResult<List<AffiliateCommission>>> GetUserCommissions([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var commissions = await _affiliateService.GetAffiliateCommissionsAsync(userId, startDate, endDate);
                return Ok(commissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des commissions");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des commissions");
            }
        }

        [HttpGet("pending-commissions")]
        [Authorize]
        public async Task<ActionResult<decimal>> GetPendingCommissions()
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var amount = await _affiliateService.CalculatePendingCommissionsAsync(userId);
                return Ok(new { PendingAmount = amount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des commissions en attente");
                return StatusCode(500, "Une erreur est survenue lors du calcul des commissions en attente");
            }
        }

        [HttpPost("request-payout")]
        [Authorize]
        public async Task<ActionResult<AffiliatePayoutRequest>> RequestPayout([FromBody] PayoutRequestData request)
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var payoutRequest = await _affiliateService.RequestPayoutAsync(userId, request.Amount);
                return Ok(payoutRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la demande de paiement");
                return StatusCode(500, "Une erreur est survenue lors de la demande de paiement");
            }
        }

        [HttpGet("statistics")]
        [Authorize]
        public async Task<ActionResult<AffiliateStatistics>> GetUserStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Récupérer l'ID de l'utilisateur connecté
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var statistics = await _affiliateService.GetAffiliateStatisticsAsync(userId, startDate, endDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques d'affiliation");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        #endregion
    }
    
    #region DTOs
    
    /// <summary>
    /// Requête pour générer un lien d'affiliation
    /// </summary>
    public class LinkGenerationRequest
    {
        /// <summary>
        /// Type de la cible (mod, abonnement, etc.)
        /// </summary>
        public string TargetType { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de la cible (mod, abonnement, etc.)
        /// </summary>
        public string TargetId { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Requête pour suivre un clic sur un lien d'affiliation
    /// </summary>
    public class ClickTrackingRequest
    {
        /// <summary>
        /// ID du lien d'affiliation
        /// </summary>
        public string LinkId { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Requête pour suivre une conversion
    /// </summary>
    public class ConversionTrackingRequest
    {
        /// <summary>
        /// ID du clic d'origine
        /// </summary>
        public string ClickId { get; set; } = string.Empty;
        
        /// <summary>
        /// Montant de la vente
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// ID de la commande liée
        /// </summary>
        public string OrderId { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Données pour une demande de paiement
    /// </summary>
    public class PayoutRequestData
    {
        /// <summary>
        /// Montant demandé
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// Méthode de paiement (PayPal, virement bancaire, etc.)
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// Informations de paiement (email PayPal, etc.)
        /// </summary>
        public string PaymentDetails { get; set; } = string.Empty;
    }
    
    #endregion
}
