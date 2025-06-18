using Frontend.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Frontend.Services;

/// <summary>
/// Implémentation du service de paiement qui interagit avec le backend Stripe
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentService> _logger;
    private readonly string _baseApiUrl;

    public PaymentService(HttpClient httpClient, IConfiguration configuration, ILogger<PaymentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseApiUrl = configuration["ApiBaseUrl"] + "/api/Stripe";
    }

    /// <inheritdoc />
    public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(PaymentIntentRequest request)
    {
        try
        {
            _logger.LogInformation("Création d'une intention de paiement pour l'utilisateur {UserId}", request.UserId);
            
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync($"{_baseApiUrl}/create-payment-intent", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erreur lors de la création de l'intention de paiement: {ErrorContent}", errorContent);
                throw new HttpRequestException($"Erreur lors de la création de l'intention de paiement: {response.StatusCode}");
            }

            var paymentIntentResponse = await response.Content.ReadFromJsonAsync<PaymentIntentResponse>();
            
            _logger.LogInformation("Intention de paiement créée avec succès: {PaymentIntentId}", 
                paymentIntentResponse?.PaymentIntentId ?? "Inconnu");
            
            return paymentIntentResponse ?? new PaymentIntentResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Échec de la création de l'intention de paiement");
            throw;
        }
    }
    
    /// <inheritdoc />
    public async Task<List<PaymentHistory>> GetPaymentHistoryAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/Stripe/payments/{userId}");
            response.EnsureSuccessStatusCode();
            
            var payments = await response.Content.ReadFromJsonAsync<List<PaymentHistory>>();
            return payments ?? new List<PaymentHistory>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erreur HTTP lors de la récupération de l'historique des paiements pour l'utilisateur {UserId}", userId);
            return new List<PaymentHistory>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur générale lors de la récupération de l'historique des paiements pour l'utilisateur {UserId}", userId);
            return new List<PaymentHistory>();
        }
    }
    
    /// <inheritdoc />
    public async Task<PaymentStatistics> GetPaymentStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Récupération des statistiques de paiement pour le tableau de bord");
            
            var response = await _httpClient.GetAsync($"{_baseApiUrl}/statistics");
            response.EnsureSuccessStatusCode();
            
            var statistics = await response.Content.ReadFromJsonAsync<PaymentStatistics>();
            return statistics ?? new PaymentStatistics();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erreur HTTP lors de la récupération des statistiques de paiement");
            return new PaymentStatistics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur générale lors de la récupération des statistiques de paiement");
            return new PaymentStatistics();
        }
    }
    
    /// <inheritdoc />
    public async Task<PaymentChartData> GetRevenueChartDataAsync(string period = "day", int count = 30)
    {
        try
        {
            _logger.LogInformation("Récupération des données de graphique de revenus pour la période {Period} ({Count})", period, count);
            
            var response = await _httpClient.GetAsync($"{_baseApiUrl}/revenue-chart?period={period}&count={count}");
            response.EnsureSuccessStatusCode();
            
            var chartData = await response.Content.ReadFromJsonAsync<PaymentChartData>();
            return chartData ?? new PaymentChartData();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erreur HTTP lors de la récupération des données de graphique de revenus");
            return new PaymentChartData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur générale lors de la récupération des données de graphique de revenus");
            return new PaymentChartData();
        }
    }
    
    /// <inheritdoc />
    public async Task<List<ReviewTransaction>> GetTransactionsRequiringReviewAsync(int minPriority = 1, int maxResults = 50)
    {
        try
        {
            _logger.LogInformation("Récupération des transactions nécessitant une révision (priorité min: {MinPriority}, max résultats: {MaxResults})", minPriority, maxResults);
            
            var response = await _httpClient.GetAsync($"{_baseApiUrl}/transactions-for-review?minPriority={minPriority}&maxResults={maxResults}");
            response.EnsureSuccessStatusCode();
            
            var transactions = await response.Content.ReadFromJsonAsync<List<ReviewTransaction>>();
            return transactions ?? new List<ReviewTransaction>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erreur HTTP lors de la récupération des transactions à examiner");
            return new List<ReviewTransaction>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur générale lors de la récupération des transactions à examiner");
            return new List<ReviewTransaction>();
        }
    }
    
    /// <inheritdoc />
    public async Task<bool> RefundPaymentAsync(string paymentId, long? amount = null, string reason = "")
    {
        try
        {
            _logger.LogInformation("Demande de remboursement pour le paiement {PaymentId} (montant: {Amount})", paymentId, amount);
            
            var request = new RefundRequest
            {
                PaymentId = paymentId,
                Amount = amount,
                Reason = reason
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync($"{_baseApiUrl}/refund", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Erreur lors du remboursement: {ErrorContent}", errorContent);
                return false;
            }

            _logger.LogInformation("Remboursement réussi pour le paiement {PaymentId}", paymentId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Échec du remboursement pour le paiement {PaymentId}", paymentId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur générale lors du remboursement pour le paiement {PaymentId}", paymentId);
            return false;
        }
    }
}
