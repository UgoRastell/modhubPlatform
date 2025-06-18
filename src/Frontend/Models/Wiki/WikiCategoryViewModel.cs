using System;
using System.Collections.Generic;

namespace Frontend.Models.Wiki
{
    public class WikiCategoryViewModel
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public int PagesCount { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public required string LastUpdateByUserId { get; set; }
        public required string LastUpdateByUserName { get; set; }
        public List<WikiPageViewModel> Pages { get; set; } = new List<WikiPageViewModel>();

        // Properties used in WikiIndex.razor
        public required string Slug { get; set; }
        public int PageCount { get; set; } // Alias for PagesCount for frontend consistency
    }
}
