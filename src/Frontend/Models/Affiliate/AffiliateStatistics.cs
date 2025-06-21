namespace Frontend.Models.Affiliate
{
    public class AffiliateStatistics
    {
        public int TotalClicks { get; set; }
        public int TotalConversions { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal TotalCommissionEarned { get; set; }
        public decimal PendingCommission { get; set; }
        public decimal PaidCommission { get; set; }

        // Additional properties referenced in AffiliateProgram.razor
        public decimal CommissionPending { get; set; } // Seems to be used interchangeably with PendingCommission
        public int CurrentLevel { get; set; }
        public int NextLevelProgress { get; set; } // Percentage progress to next level
    }
}
