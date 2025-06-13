using System;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        public string FirstName { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string LastName { get; set; } = string.Empty;
        
        public string AvatarUrl { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
    }
}
