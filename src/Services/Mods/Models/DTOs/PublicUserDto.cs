namespace ModsService.Models.DTOs
{
    /// <summary>
    /// Minimal public user representation retrieved from UsersService.
    /// </summary>
    public class PublicUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
    }
}
