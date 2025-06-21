using Frontend.Models.Affiliate;
using Frontend.Models.Affiliate.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services.Affiliate
{
    public interface IAffiliateService
    {
        Task<AffiliateProgramInfo> GetAffiliateProgramInfoAsync();
        Task<List<AffiliateLink>> GetAffiliateLinksAsync();
        Task<List<AffiliateCommission>> GetCommissionsAsync();
        Task<AffiliateStatistics> GetStatisticsAsync();
        
        // Méthodes manquantes détectées dans AffiliateProgram.razor
        Task<AffiliateProgramInfo> GetProgramDetailsAsync();
        Task<AffiliateUserStatistics> GetUserStatisticsAsync();
        Task<List<AffiliateLink>> GetUserAffiliateLinksAsync();
        Task<List<AffiliateCommission>> GetUserCommissionsAsync();
        Task<AffiliateLinkStatistics> GetLinkStatisticsAsync(string linkId);
        Task<AffiliateLink> GenerateAffiliateLinkAsync(LinkGenerationRequest request);
        Task<bool> RequestPayoutAsync(PayoutRequestData request);
        Task<bool> CopyToClipboardAsync(string text);
    }
}
