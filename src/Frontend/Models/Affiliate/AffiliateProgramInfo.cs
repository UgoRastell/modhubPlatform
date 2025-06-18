namespace Frontend.Models.Affiliate
{
    public class AffiliateProgramInfo
    {
        public required string ProgramName { get; set; }
        public required string Description { get; set; }
        public decimal CommissionRate { get; set; }
        public int MinimumPayout { get; set; }
        public required string TermsAndConditions { get; set; }
        
        // Additional properties referenced in AffiliateProgram.razor
        public bool AllowCustomCommission { get; set; }
        public decimal MaxCustomCommission { get; set; }
        public decimal CommissionPercentage { get; set; } // Seems to be used interchangeably with CommissionRate
        public decimal MinimumPayoutAmount { get; set; } // Seems to be used interchangeably with MinimumPayout
        public int CookieDuration { get; set; } // In days
    }
}
