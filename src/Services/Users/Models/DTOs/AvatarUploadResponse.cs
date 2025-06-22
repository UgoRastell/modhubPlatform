namespace UsersService.Models.DTOs
{
    public class AvatarUploadResponse
    {
        public string AvatarUrl { get; set; } = string.Empty;
        public string Message { get; set; } = "Avatar mis à jour avec succès";
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
