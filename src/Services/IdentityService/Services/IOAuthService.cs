using IdentityService.Models;

namespace IdentityService.Services
{
    public interface IOAuthService
    {
        Task<AuthResponse> AuthenticateAsync(string provider, string token);
        Task<TokenResponse> ExchangeGoogleAuthCodeForTokenAsync(string code);
        Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken);
        Task<AuthResponse> AuthenticateGoogleUserAsync(GoogleUserInfo userInfo);
    }
}
