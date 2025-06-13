using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Handlers;

public class JwtAuthenticationHandler : AuthenticationHandler<JwtAuthenticationOptions>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtAuthenticationHandler> _logger;

    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration) : base(options, logger, encoder)
    {
        _configuration = configuration;
        _logger = logger.CreateLogger<JwtAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Si pas d'autorisation dans l'entête, pas de token à valider
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            _logger.LogInformation("Request does not contain Authorization header");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string authorizationHeader = Request.Headers["Authorization"];
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Authorization header is empty or not Bearer token");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string token = authorizationHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Token is empty");
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            return ValidateTokenAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }
    }

    private Task<AuthenticateResult> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Key"] ?? "DefaultSecureKeyWithAtLeast32Characters!");

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["JwtSettings:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            // Valider le token
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // Créer le ticket d'authentification
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            _logger.LogInformation("Token validated successfully for user {UserId}", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token has expired");
            return Task.FromResult(AuthenticateResult.Fail("Token has expired"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }
    }
}
