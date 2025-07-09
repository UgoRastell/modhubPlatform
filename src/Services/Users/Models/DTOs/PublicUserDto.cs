namespace UsersService.Models.DTOs
{
    /// <summary>
    /// Représentation publique d'un utilisateur destinée à être exposée aux autres services / clients.
    /// Contient uniquement les informations non sensibles afin de respecter la confidentialité et le RGPD.
    /// </summary>
    public class PublicUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}
