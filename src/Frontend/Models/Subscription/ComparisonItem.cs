namespace Frontend.Models.Subscription
{
    public class ComparisonItem
    {
        public required string Feature { get; set; }
        public bool IsIncluded { get; set; }
        public required string Description { get; set; }
        public bool IsKeyFeature { get; set; }
        public string FeatureName { get; set; } = string.Empty;
        public Dictionary<string, string> TierValues { get; set; } = new Dictionary<string, string>();
    }
}
