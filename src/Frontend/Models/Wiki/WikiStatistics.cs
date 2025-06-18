namespace Frontend.Models.Wiki
{
    public class WikiStatistics
    {
        public int TotalCategories { get; set; }
        public int TotalPages { get; set; }
        public int TotalContributors { get; set; }
        public int TotalViews { get; set; }
        public int TotalRevisions { get; set; } // Property referenced in WikiIndex.razor
        public required string MostViewedPage { get; set; }
        public required string MostViewedPageId { get; set; }
        public required string MostActiveCategory { get; set; }
        public required string MostActiveCategoryId { get; set; }
        public required string MostActiveContributor { get; set; }
        public required string MostActiveContributorId { get; set; }
    }
}
