namespace Frontend.Models
{
    public class DataRetentionSettingsDto
    {
        public int DetailedHistoryRetentionDays { get; set; } = 90;
        public int AggregationThresholdDays { get; set; } = 30;
        public bool AggregateBeforeDelete { get; set; } = true;
    }
}
