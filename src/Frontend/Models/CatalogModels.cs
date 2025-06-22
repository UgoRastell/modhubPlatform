using System;
using System.Collections.Generic;

namespace Frontend.Models
{
    public class ModItem
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string ThumbnailUrl { get; set; }
        public required string Author { get; set; }
        public required string AuthorId { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public int DownloadCount { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsPremium { get; set; }
        public bool IsNew { get; set; } // Moins de 7 jours
        public bool IsPopular { get; set; } // Top 10% en téléchargements
        public required string GameName { get; set; }
        public required string GameId { get; set; }
        public required List<string> Tags { get; set; }
        public bool IsFavorite { get; set; }
    }

    public class CatalogFilter
    {
        public required string SearchText { get; set; } = string.Empty;
        public List<string> GameIds { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public bool ShowPremiumOnly { get; set; }
        public bool ShowNewOnly { get; set; }
        public bool ShowPopularOnly { get; set; }
        public bool FavoritesOnly { get; set; }
        public SortOption SortBy { get; set; } = SortOption.Popularity;
    }

    public enum SortOption
    {
        Popularity,
        RecentlyUpdated,
        NewReleases,
        TopRated,
        MostDownloaded,
        AlphabeticalAZ,
        AlphabeticalZA
    }

    public class Game
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public string ShortDescription { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int ModCount { get; set; }
    }

    public class Tag
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public int ModCount { get; set; }
    }

    public class GameOption
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
    }

    public class TagOption
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public int ModCount { get; set; }
    }
}
