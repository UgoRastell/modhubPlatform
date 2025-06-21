namespace Frontend.Models.Affiliate.Requests
{
    public class LinkGenerationRequest
    {
        public required string ModId { get; set; }
        public required string CustomTag { get; set; }

        // Properties used in GenerateAffiliateLinkDialog.razor
        public string TargetType { get; set; } = "mod";
        public string TargetId { get; set; } = string.Empty;
        public string? CustomLabel { get; set; }
        public string? UtmSource { get; set; }
        public string? UtmMedium { get; set; }
        public string? UtmCampaign { get; set; }
        public decimal? CustomCommissionPercentage { get; set; }
    }
}
