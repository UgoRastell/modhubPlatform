using System;

namespace Frontend.Models.Subscription
{
    public class SubscriptionStatus
    {
        public bool IsSubscribed { get; set; }
        public required string CurrentTierId { get; set; }
        public required string CurrentTierName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool AutoRenew { get; set; }
        public bool IsCancelled { get; set; }
    }
}
