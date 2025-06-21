using System;

namespace Frontend.Models.Affiliate
{
    public class AffiliateCommission
    {
        public required string Id { get; set; }
        public required string ModId { get; set; }
        public required string ModName { get; set; }
        public required string UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPaid { get; set; }
        
        // Additional properties referenced in AffiliateProgram.razor
        public required string Status { get; set; } = "Pending"; // Default status
        public string? Description { get; set; } // Description of the commission
        public string? LinkId { get; set; } // Referenced affiliate link
        public string? PaymentId { get; set; } // Payment ID when paid
    }
}
