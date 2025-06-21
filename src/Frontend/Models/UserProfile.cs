using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Le nom d'utilisateur doit contenir entre 3 et 32 caractères")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        public string FirstName { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string LastName { get; set; } = string.Empty;
        
        public string AvatarUrl { get; set; } = string.Empty;

        [StringLength(280, ErrorMessage = "La biographie ne peut pas dépasser 280 caractères")]
        public string ShortBio { get; set; } = string.Empty;
        
        // Alias for Bio to maintain compatibility with existing code
        public string Bio { 
            get => ShortBio; 
            set => ShortBio = value; 
        }

        public List<ExternalLink> ExternalLinks { get; set; } = new List<ExternalLink>();
        
        // Website shortcut property (first external link of type website)
        public string Website {
            get => ExternalLinks?.FirstOrDefault(l => l.Type == "website")?.Url ?? string.Empty;
            set {
                var link = ExternalLinks?.FirstOrDefault(l => l.Type == "website");
                if (link != null)
                    link.Url = value;
                else
                    ExternalLinks?.Add(new ExternalLink { Type = "website", Url = value, Icon = "fas fa-globe" });
            }
        }
        
        public string Location { get; set; } = string.Empty;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }

        // Statistiques rapides
        public int ModsCount { get; set; }
        public int DownloadsCount { get; set; }
        public int FollowersCount { get; set; }
        public int ReputationScore { get; set; }

        // Slug pour l'URL
        public string Slug => !string.IsNullOrEmpty(Username) ? 
            Username.ToLower().Replace(" ", "-") : string.Empty;
    }

    public class ExternalLink
    {
        public string Type { get; set; } = string.Empty; // discord, twitch, youtube, website
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
