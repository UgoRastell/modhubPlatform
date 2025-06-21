using PaymentsService.Models;

namespace PaymentsService.Services
{
    public interface IUserService
    {
        Task<UserInfo> GetUserByIdAsync(string userId);
        Task<UserInfo> GetUserByEmailAsync(string email);
        Task<string> GetUserEmailAsync(string userId);
        Task NotifyUserAsync(string userId, string subject, string message);
    }
    
    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
