using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IOAuthService _oauthService;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ITwoFactorService twoFactorService, 
            IOAuthService oauthService,
            IRabbitMQService rabbitMQService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _twoFactorService = twoFactorService;
            _oauthService = oauthService;
            _rabbitMQService = rabbitMQService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var result = await _authService.RegisterUserAsync(request);
                
                // Publish UserCreated event
                await _rabbitMQService.PublishAsync("user.events", new UserCreatedEvent 
                {
                    UserId = result.UserId,
                    Username = request.Username,
                    Email = request.Email,
                    CreatedAt = DateTime.UtcNow
                });

                return Ok(result);
            }
            catch (UserExistsException)
            {
                return Conflict(new { message = "Username or email already exists." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return StatusCode(500, new { message = "An error occurred while registering." });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.AuthenticateAsync(request.Username, request.Password);
                
                if (result.RequiresTwoFactor)
                {
                    // Return 2FA required response
                    return Ok(new { requiresTwoFactor = true, temporaryToken = result.Token });
                }

                // Publish UserLoggedIn event
                await _rabbitMQService.PublishAsync("user.events", new UserLoggedInEvent 
                {
                    UserId = result.UserId,
                    Username = request.Username,
                    LoginTime = DateTime.UtcNow
                });

                return Ok(result);
            }
            catch (InvalidCredentialsException)
            {
                return Unauthorized(new { message = "Invalid username or password." });
            }
            catch (AccountLockedException)
            {
                return Unauthorized(new { message = "Account is locked. Please try again later or contact support." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "An error occurred during login." });
            }
        }

        [HttpPost("validate-2fa")]
        public async Task<ActionResult<AuthResponse>> ValidateTwoFactor([FromBody] TwoFactorRequest request)
        {
            try
            {
                var result = await _twoFactorService.ValidateAsync(request.TemporaryToken, request.TwoFactorCode);
                return Ok(result);
            }
            catch (InvalidTwoFactorException)
            {
                return Unauthorized(new { message = "Invalid two-factor code." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating 2FA");
                return StatusCode(500, new { message = "An error occurred while validating two-factor authentication." });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(result);
            }
            catch (InvalidTokenException)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "An error occurred during token refresh." });
            }
        }

        [HttpPost("oauth/{provider}")]
        public async Task<ActionResult<AuthResponse>> OAuthLogin(string provider, [FromBody] OAuthRequest request)
        {
            try
            {
                var result = await _oauthService.AuthenticateAsync(provider, request.Token);
                
                // If this is a new user, publish user created event
                if (result.IsNewUser)
                {
                    await _rabbitMQService.PublishAsync("user.events", new UserCreatedEvent
                    {
                        UserId = result.UserId,
                        Username = result.Username,
                        Email = result.Email,
                        CreatedAt = DateTime.UtcNow,
                        OAuthProvider = provider
                    });
                }

                // Publish UserLoggedIn event
                await _rabbitMQService.PublishAsync("user.events", new UserLoggedInEvent
                {
                    UserId = result.UserId,
                    Username = result.Username,
                    LoginTime = DateTime.UtcNow,
                    OAuthProvider = provider
                });

                return Ok(result);
            }
            catch (UnsupportedOAuthProviderException)
            {
                return BadRequest(new { message = $"OAuth provider '{provider}' is not supported." });
            }
            catch (OAuthValidationException)
            {
                return Unauthorized(new { message = "OAuth validation failed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OAuth login");
                return StatusCode(500, new { message = "An error occurred during OAuth authentication." });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                await _authService.RevokeTokensForUserAsync(userId);
                return Ok(new { message = "Successfully logged out" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout." });
            }
        }

        [HttpDelete("account")]
        [Authorize]
        public async Task<ActionResult> DeleteAccount()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                await _authService.DeleteUserAsync(userId);
                
                // Publish UserDeleted event
                await _rabbitMQService.PublishAsync("user.events", new UserDeletedEvent
                {
                    UserId = userId,
                    DeletedAt = DateTime.UtcNow
                });

                return Ok(new { message = "Account successfully deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account");
                return StatusCode(500, new { message = "An error occurred while deleting the account." });
            }
        }
    }
}
