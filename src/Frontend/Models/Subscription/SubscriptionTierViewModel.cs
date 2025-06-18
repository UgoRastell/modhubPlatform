using System.Collections.Generic;

namespace Frontend.Models.Subscription
{
    public class SubscriptionTierViewModel
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public required string BillingPeriod { get; set; }
        public int DurationInDays { get; set; }
        public int DisplayOrder { get; set; }
        public List<ComparisonItem> Features { get; set; } = new List<ComparisonItem>();
        
        // Propriétés manquantes utilisées dans SubscriptionPlans.razor
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public bool IsRecommended { get; set; }
        public string ThemeColor { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int TrialPeriodDays { get; set; }
        public List<string> Benefits { get; set; } = new List<string>();
    }
}
