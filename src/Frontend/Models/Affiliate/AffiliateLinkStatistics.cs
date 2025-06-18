using System;
using System.Collections.Generic;

namespace Frontend.Models.Affiliate
{
    public class AffiliateLinkStatistics
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Clicks { get; set; }
        public int Conversions { get; set; }
        public decimal ConversionRate => Clicks > 0 ? (decimal)Conversions / Clicks : 0;
        public decimal TotalCommissions { get; set; }
        public List<int> DailyClicks { get; set; } = new List<int>();
        public List<int> DailyConversions { get; set; } = new List<int>();
    }
}
