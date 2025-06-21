using System;

namespace Frontend.Models
{
    public class QuotaEntryDto
    {
        public string? Id { get; set; }
        public string? Identifier { get; set; }
        public string? Type { get; set; }
        public int DailyQuota { get; set; }
        public int CurrentCount { get; set; }
        public DateTime LastReset { get; set; }
        public bool IsPremium { get; set; }
        public bool IsPriority { get; set; }
    }
}
