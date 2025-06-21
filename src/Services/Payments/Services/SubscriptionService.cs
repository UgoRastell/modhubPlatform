using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentsService.Config;
using PaymentsService.Models.DTOs;
using Stripe;

namespace PaymentsService.Services
{
    /// <summary>
    /// Service de gestion des abonnements via Stripe
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ILogger<SubscriptionService> _logger;
        private readonly StripeOptions _stripeOptions;
        private readonly IStripeClient? _stripeClient;
        
        /// <summary>
        /// Constructeur du service d'abonnements
        /// </summary>
        public SubscriptionService(
            ILogger<SubscriptionService> logger,
            IOptions<StripeOptions> stripeOptions,
            IStripeClient? stripeClient = null)
        {
            _logger = logger;
            _stripeOptions = stripeOptions.Value;
            _stripeClient = stripeClient ?? new StripeClient(_stripeOptions.SecretKey);
            
            // Configuration du client Stripe global
            StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
        }

        /// <inheritdoc />
        public async Task<SubscriptionResponse> CreateSubscriptionAsync(PaymentIntentRequest request)
        {
            try
            {
                _logger.LogInformation("Création d'un abonnement pour l'utilisateur {UserId}", request.UserId);
                
                if (!request.IsRecurring || !request.Frequency.HasValue)
                {
                    throw new ArgumentException("La demande doit être récurrente et avoir une fréquence définie");
                }
                
                // Convertir la fréquence au format Stripe
                string interval = ConvertFrequencyToStripeInterval(request.Frequency.Value);
                
                // Trouver ou créer le client Stripe
                var customerService = new CustomerService(_stripeClient);
                var customerOptions = new CustomerSearchOptions
                {
                    Query = $"metadata['user_id']:'{request.UserId}'"
                };
                var customers = await customerService.SearchAsync(customerOptions);
                
                Customer customer;
                if (customers.Data.Count > 0)
                {
                    customer = customers.Data.First();
                }
                else
                {
                    // Créer un nouveau client
                    var customerCreateOptions = new CustomerCreateOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            { "user_id", request.UserId },
                            { "app_environment", _stripeOptions.Environment ?? "development" }
                        }
                    };
                    customer = await customerService.CreateAsync(customerCreateOptions);
                }
                
                // Créer un produit si nécessaire
                var productService = new ProductService(_stripeClient);
                var productOptions = new ProductSearchOptions
                {
                    Query = $"metadata['product_id']:'{request.ProductId}'"
                };
                var products = await productService.SearchAsync(productOptions);
                
                Product product;
                if (products.Data.Count > 0)
                {
                    product = products.Data.First();
                }
                else
                {
                    // Créer un nouveau produit
                    var productCreateOptions = new ProductCreateOptions
                    {
                        Name = request.Description,
                        Description = $"Abonnement pour {request.Description}",
                        Metadata = new Dictionary<string, string>
                        {
                            { "product_id", request.ProductId },
                            { "app_environment", _stripeOptions.Environment ?? "development" }
                        }
                    };
                    product = await productService.CreateAsync(productCreateOptions);
                }
                
                // Créer un prix pour le produit
                var priceService = new PriceService(_stripeClient);
                var priceCreateOptions = new PriceCreateOptions
                {
                    UnitAmount = request.Amount,
                    Currency = request.Currency,
                    Product = product.Id,
                    Recurring = new PriceRecurringOptions
                    {
                        Interval = interval,
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", request.UserId },
                        { "product_id", request.ProductId },
                        { "app_environment", _stripeOptions.Environment ?? "development" }
                    }
                };
                var price = await priceService.CreateAsync(priceCreateOptions);
                
                // Créer ou récupérer un abonnement
                var subscriptionService = new Stripe.SubscriptionService(_stripeClient);
                var options = new SubscriptionCreateOptions
                {
                    Customer = customer.Id,
                    Items = new List<SubscriptionItemOptions>
                    {
                        new SubscriptionItemOptions
                        {
                            Price = price.Id,
                        }
                    },
                    PaymentBehavior = "default_incomplete",
                    PaymentSettings = new SubscriptionPaymentSettingsOptions
                    {
                        SaveDefaultPaymentMethod = "on_subscription",
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", request.UserId },
                        { "product_id", request.ProductId },
                        { "app_environment", _stripeOptions.Environment ?? "development" }
                    },
                    Expand = new List<string> { "latest_invoice.payment_intent" }
                };
                var subscription = await subscriptionService.CreateAsync(options);
                
                // Extraire le client secret pour le paiement frontend
                var invoice = subscription.LatestInvoice;
                string? clientSecret = null;
                
                // Récupérer le PaymentIntent de la facture si nécessaire
                if (invoice != null)
                {
                    // Essayer d'accéder au PaymentIntent
                    
                    // Dans Stripe.NET v48.2.0, nous devons obtenir le PaymentIntent d'une autre manière
                    var invoiceService = new InvoiceService(_stripeClient);
                    var completeInvoice = await invoiceService.GetAsync(invoice.Id);
                    
                    // Utiliser la réflexion pour accéder à la propriété PaymentIntent 
                    var paymentIntentId = string.Empty;
                    var prop = completeInvoice.GetType().GetProperty("PaymentIntent");
                    if (prop != null)
                    {
                        var paymentIntent = prop.GetValue(completeInvoice);
                        if (paymentIntent != null)
                        {
                            var idProp = paymentIntent.GetType().GetProperty("Id");
                            if (idProp != null)
                            {
                                paymentIntentId = idProp.GetValue(paymentIntent) as string;
                            }
                        }
                    }
                    
                    // Si on ne trouve pas par réflexion, essayer d'utiliser les metadonnées
                    if (string.IsNullOrEmpty(paymentIntentId) && completeInvoice.Metadata != null && 
                        completeInvoice.Metadata.TryGetValue("payment_intent_id", out var metaPayId))
                    {
                        paymentIntentId = metaPayId;
                    }
                    
                    if (!string.IsNullOrEmpty(paymentIntentId))
                    {
                        var paymentIntentService = new PaymentIntentService(_stripeClient);
                        var paymentIntent = await paymentIntentService.GetAsync(paymentIntentId);
                        clientSecret = paymentIntent?.ClientSecret;
                    }
                    // Ce bloc a été fusionné avec le cas précédent
                }
                
                // Créer la réponse
                var response = new SubscriptionResponse
                {
                    SubscriptionId = subscription.Id,
                    CustomerId = customer.Id,
                    ProductId = request.ProductId,
                    Status = subscription.Status,
                    // Dans Stripe.NET v48.2.0, nous utilisons les propriétés disponibles pour les dates
                    // Assignation directe des objets DateTime sans formatage en string
                    CurrentPeriodStart = subscription.Created,
                    // Par défaut, on ajoute 30 jours à la date de création si on ne peut pas accéder à la fin
                    CurrentPeriodEnd = subscription.Created.AddDays(30),
                    Amount = request.Amount,
                    Currency = request.Currency,
                    CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                    Interval = interval,
                    ClientSecret = clientSecret ?? string.Empty,
                    PublishableKey = _stripeOptions.PublishableKey
                };
                
                return response;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erreur Stripe lors de la création d'un abonnement: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'un abonnement: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Subscription> GetSubscriptionAsync(string subscriptionId)
        {
            try
            {
                var subscriptionService = new Stripe.SubscriptionService(_stripeClient);
                return await subscriptionService.GetAsync(subscriptionId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erreur Stripe lors de la récupération de l'abonnement {SubscriptionId}: {Message}", subscriptionId, ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<Subscription>> GetUserSubscriptionsAsync(string userId)
        {
            try
            {
                var subscriptions = new List<Subscription>();
                
                // Rechercher le client par user_id
                var customerService = new CustomerService(_stripeClient);
                var customerOptions = new CustomerSearchOptions
                {
                    Query = $"metadata['user_id']:'{userId}'"
                };
                var customers = await customerService.SearchAsync(customerOptions);
                
                if (!customers.Data.Any())
                {
                    return subscriptions;
                }
                
                // Pour chaque client trouvé, récupérer les abonnements
                var subscriptionService = new Stripe.SubscriptionService(_stripeClient);
                foreach (var customer in customers.Data)
                {
                    var listOptions = new SubscriptionListOptions
                    {
                        Customer = customer.Id,
                        Status = "all",
                        Limit = 100 // Nombre max d'abonnements à récupérer
                    };
                    var customerSubscriptions = await subscriptionService.ListAsync(listOptions);
                    subscriptions.AddRange(customerSubscriptions.Data);
                }
                
                return subscriptions;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erreur Stripe lors de la récupération des abonnements pour l'utilisateur {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Subscription> CancelSubscriptionAsync(string subscriptionId, bool cancelImmediately = false)
        {
            try
            {
                // Instancier un nouveau service d'abonnement
                var subscriptionService = new Stripe.SubscriptionService(_stripeClient);
                
                if (cancelImmediately)
                {
                    // Annulation immédiate
                    return await subscriptionService.CancelAsync(subscriptionId, new SubscriptionCancelOptions());
                }
                else
                {
                    // Annulation à la fin de la période
                    var options = new SubscriptionUpdateOptions
                    {
                        CancelAtPeriodEnd = true
                    };
                    return await subscriptionService.UpdateAsync(subscriptionId, options);
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erreur Stripe lors de l'annulation de l'abonnement {SubscriptionId}: {Message}", subscriptionId, ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Subscription> UpdateSubscriptionAsync(string subscriptionId, SubscriptionUpdateRequest updateRequest)
        {
            try
            {
                // Instancier un nouveau service d'abonnement
                var subscriptionService = new Stripe.SubscriptionService(_stripeClient);
            
                var subscription = await subscriptionService.GetAsync(subscriptionId);
                
                // Préparer les options de mise à jour
                var updateOptions = new SubscriptionUpdateOptions();
                
                // Appliquer les changements demandés
                if (!string.IsNullOrEmpty(updateRequest.NewPlanId))
                {
                    // Trouver l'ID de l'élément d'abonnement à modifier
                    var itemId = subscription.Items.Data.FirstOrDefault()?.Id;
                    if (itemId != null)
                    {
                        updateOptions.Items = new List<SubscriptionItemOptions>
                        {
                            new SubscriptionItemOptions
                            {
                                Id = itemId,
                                Price = updateRequest.NewPlanId
                            }
                        };
                    }
                }
                
                if (updateRequest.Quantity.HasValue && updateRequest.Quantity > 0)
                {
                    // Mettre à jour la quantité
                    var itemId = subscription.Items.Data.FirstOrDefault()?.Id;
                    if (itemId != null)
                    {
                        if (updateOptions.Items == null)
                        {
                            updateOptions.Items = new List<SubscriptionItemOptions>();
                        }
                        
                        // Vérifier si l'élément existe déjà dans la liste
                        var existingItem = updateOptions.Items.FirstOrDefault(i => i.Id == itemId);
                        if (existingItem != null)
                        {
                            existingItem.Quantity = updateRequest.Quantity;
                        }
                        else
                        {
                            updateOptions.Items.Add(new SubscriptionItemOptions
                            {
                                Id = itemId,
                                Quantity = updateRequest.Quantity
                            });
                        }
                    }
                }
                
                if (updateRequest.CancelAtPeriodEnd.HasValue)
                {
                    updateOptions.CancelAtPeriodEnd = updateRequest.CancelAtPeriodEnd;
                }
                
                if (!string.IsNullOrEmpty(updateRequest.PaymentMethodId))
                {
                    updateOptions.DefaultPaymentMethod = updateRequest.PaymentMethodId;
                }
                
                if (updateRequest.Metadata != null && updateRequest.Metadata.Count > 0)
                {
                    updateOptions.Metadata = new Dictionary<string, string>(updateRequest.Metadata);
                }
                
                // Appliquer les modifications
                return await subscriptionService.UpdateAsync(subscriptionId, updateOptions);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Erreur Stripe lors de la mise à jour de l'abonnement {SubscriptionId}: {Message}", subscriptionId, ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ProcessSubscriptionEventAsync(Event stripeEvent)
        {
            await Task.CompletedTask; // Pour résoudre l'avertissement CS1998
            try
            {
                switch (stripeEvent.Type)
                {
                    case "customer.subscription.created":
                    case "customer.subscription.updated":
                    case "customer.subscription.deleted":
                        var subscription = stripeEvent.Data.Object as Subscription;
                        if (subscription != null)
                        {
                            _logger.LogInformation("Événement d'abonnement traité: {EventType}, SubscriptionId: {SubscriptionId}", 
                                stripeEvent.Type, subscription.Id);
                            
                            // Ici, vous pourriez mettre à jour votre base de données, envoyer des notifications, etc.
                            
                            // Pour cet exemple, nous ne faisons que loguer l'événement
                            return true;
                        }
                        break;
                        
                    case "invoice.payment_succeeded":
                    case "invoice.payment_failed":
                        var invoice = stripeEvent.Data.Object as Invoice;
                        // Extraire l'ID d'abonnement via les méthodes sécurisées
                        string? subscriptionId = null;
                        if (invoice != null && !string.IsNullOrEmpty(invoice.Id))
                        {
                            try {
                                // Dans Stripe.NET v48.2.0, nous devons extraire les propriétés par réflexion
                                // ou utiliser les services appropriés
                                var invoiceService = new InvoiceService(_stripeClient);
                                var fullInvoice = await invoiceService.GetAsync(invoice.Id);
                                
                                // Vérifier si une propriété Subscription est disponible
                                var prop = fullInvoice.GetType().GetProperty("SubscriptionId");
                                if (prop != null)
                                {
                                    var value = prop.GetValue(fullInvoice) as string;
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        subscriptionId = value;
                                    }
                                }
                                
                                // Alternative: chercher dans les métadonnées
                                if (string.IsNullOrEmpty(subscriptionId) && fullInvoice.Metadata != null && 
                                    fullInvoice.Metadata.TryGetValue("subscription_id", out var metaSubId))
                                {
                                    subscriptionId = metaSubId;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Erreur lors de la récupération des détails de la facture {InvoiceId}", invoice.Id);
                            }
                        }
                        
                        if (invoice != null && !string.IsNullOrEmpty(subscriptionId)) // Version corrigée
                        {
                            _logger.LogInformation("Événement de facturation d'abonnement traité: {EventType}, SubscriptionId: {SubscriptionId}", 
                                stripeEvent.Type, subscriptionId);
                            
                            // Traitement spécifique selon le type d'événement
                            if (stripeEvent.Type == "invoice.payment_failed")
                            {
                                // Logique pour gérer les échecs de paiement
                                _logger.LogWarning("Échec de paiement pour l'abonnement {SubscriptionId}", subscriptionId);
                                
                                // Notification à l'utilisateur, etc.
                            }
                            
                            return true;
                        }
                        break;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement d'un événement d'abonnement: {Message}", ex.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Convertit la fréquence d'abonnement en intervalle Stripe
        /// </summary>
        private string ConvertFrequencyToStripeInterval(RecurringFrequency frequency)
        {
            return frequency switch
            {
                RecurringFrequency.Monthly => "month",
                RecurringFrequency.Quarterly => "quarter",
                RecurringFrequency.Yearly => "year",
                _ => throw new ArgumentOutOfRangeException(nameof(frequency), "Fréquence non supportée")
            };
        }
    }
}
