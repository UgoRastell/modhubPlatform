using Frontend.Models.Subscription;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services.Subscription
{
    public interface ISubscriptionService
    {
        Task<List<SubscriptionTierViewModel>> GetSubscriptionTiersAsync();
        Task<SubscriptionStatus> GetUserSubscriptionStatusAsync();
        
        // Méthodes manquantes détectées dans SubscriptionPlans.razor
        Task<List<SubscriptionTierViewModel>> GetAllSubscriptionTiersAsync();
        Task<List<ComparisonItem>> CompareTiersAsync(List<string> tierIds);
    }
}
