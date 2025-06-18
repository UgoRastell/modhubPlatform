using System;

namespace Frontend.Models.Affiliate
{
    public class AffiliateUserStatistics
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalClicks { get; set; }
        public int TotalConversions { get; set; }
        public decimal ConversionRate => TotalClicks > 0 ? (decimal)TotalConversions / TotalClicks : 0;
        public decimal CommissionEarned { get; set; }
        public decimal CommissionPending { get; set; }
        public decimal CommissionPaid { get; set; }
        public decimal TotalCommissionEarned { get; set; }
        public int CurrentLevel { get; set; }
        public float NextLevelProgress { get; set; }
    }
}
