namespace Frontend.Models
{
    public class QuotaSettingsDto
    {
        public int AnonymousQuotaPerDay { get; set; } = 5;
        public int RegisteredQuotaPerDay { get; set; } = 20;
        public int PremiumQuotaPerDay { get; set; } = 100;
        public int IpRateLimitPerHour { get; set; } = 10;
    }
}
