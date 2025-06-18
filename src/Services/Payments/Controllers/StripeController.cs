using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaymentsService.Models.DTOs;
using PaymentsService.Services;
using Stripe;
using System.IO;
using System.Threading.Tasks;

namespace PaymentsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<StripeController> _logger;

    public StripeController(IStripeService stripeService, ILogger<StripeController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    /// <summary>
    /// Crée un PaymentIntent pour initier un paiement
    /// </summary>
    /// <param name="request">Informations nécessaires pour créer le PaymentIntent</param>
    /// <returns>Informations du PaymentIntent créé</returns>
    [HttpPost("payment-intent")]
    public async Task<ActionResult<PaymentIntentResponse>> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
    {
        try
        {
            var response = await _stripeService.CreatePaymentIntentAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création du PaymentIntent");
            return StatusCode(500, new { message = "Erreur lors de la création du paiement" });
        }
    }

    /// <summary>
    /// Point de terminaison pour recevoir les webhooks Stripe
    /// </summary>
    /// <returns>OK si le webhook a été traité avec succès</returns>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        if (!Request.Headers.TryGetValue("Stripe-Signature", out var signatureValues) || string.IsNullOrEmpty(signatureValues))
        {
            return BadRequest(new { message = "Signature Stripe manquante" });
        }
        
        string signature = signatureValues.ToString();
        
        try
        {
            var success = await _stripeService.ProcessWebhookEventAsync(json, signature);
            
            if (success)
                return Ok();
            else
                return BadRequest(new { message = "Échec du traitement du webhook" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du traitement du webhook Stripe");
            return StatusCode(500, new { message = "Erreur serveur lors du traitement du webhook" });
        }
    }

    /// <summary>
    /// Récupère l'historique des paiements d'un utilisateur
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Liste des paiements de l'utilisateur</returns>
    [Authorize]
    [HttpGet("payments/{userId}")]
    public async Task<ActionResult<IEnumerable<PaymentIntent>>> GetUserPayments(string userId)
    {
        // Vérifier que l'utilisateur authentifié est bien celui qui fait la demande
        var authenticatedUserId = User.FindFirst("sub")?.Value;
        if (authenticatedUserId != userId)
        {
            return Forbid();
        }
        
        try
        {
            var payments = await _stripeService.GetUserPaymentsAsync(userId);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des paiements de l'utilisateur");
            return StatusCode(500, new { message = "Erreur lors de la récupération des paiements" });
        }
    }
}
