using System;

namespace Frontend.Models.Affiliate
{
    public class AffiliateLink
    {
        public required string Id { get; set; }
        public required string UserId { get; set; }
        public required string ModId { get; set; }
        public required string ModName { get; set; }
        public required string LinkCode { get; set; }
        public required string CustomTag { get; set; }
        public int Clicks { get; set; }
        public int Conversions { get; set; }
        public DateTime CreatedAt { get; set; }

        // Additional properties referenced in AffiliateProgram.razor
        public required string TargetType { get; set; } = "mod"; // Default value
        public required string TargetId { get; set; } = string.Empty; // Will be set based on ModId
        public required string FullUrl { get; set; } = string.Empty; // Complete affiliate URL
        
        // Properties that seem to be used interchangeably with existing ones
        public int TotalClicks => Clicks;
        public int TotalConversions => Conversions;
    }
}
