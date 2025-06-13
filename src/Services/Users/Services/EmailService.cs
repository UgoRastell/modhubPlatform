using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UsersService.Configuration;

namespace UsersService.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlContent);
    Task<bool> SendVerificationEmailAsync(string to, string username, string token);
    Task<bool> SendPasswordResetEmailAsync(string to, string username, string token);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailSettings _emailSettings;
    private readonly IConfiguration _configuration;

    public EmailService(
        ILogger<EmailService> logger,
        IOptions<EmailSettings> emailSettings,
        IConfiguration configuration)
    {
        _logger = logger;
        _emailSettings = emailSettings.Value;
        _configuration = configuration;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent)
    {
        try
        {
            var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword)
            };

            using var message = new MailMessage(
                from: _emailSettings.FromEmail,
                to: to,
                subject: subject,
                body: htmlContent)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email envoyé à {To} avec le sujet {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi de l'email à {To}: {Error}", to, ex.Message);
            return false;
        }
    }

    public async Task<bool> SendVerificationEmailAsync(string to, string username, string token)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://modhub.io";
        var verificationLink = $"{baseUrl}/verify-email?email={WebUtility.UrlEncode(to)}&token={WebUtility.UrlEncode(token)}";
        
        var htmlContent = $@"
        <html>
        <body>
            <h1>Bienvenue sur ModHub, {WebUtility.HtmlEncode(username)} !</h1>
            <p>Merci de vous être inscrit sur ModHub. Pour activer votre compte, veuillez vérifier votre adresse email en cliquant sur le lien ci-dessous :</p>
            <p><a href='{verificationLink}'>Vérifier mon adresse email</a></p>
            <p>Si vous n'avez pas créé de compte sur ModHub, vous pouvez ignorer cet email.</p>
            <p>Cordialement,<br>L'équipe ModHub</p>
        </body>
        </html>";
        
        return await SendEmailAsync(to, "ModHub - Vérification de votre adresse email", htmlContent);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string username, string token)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://modhub.io";
        var resetLink = $"{baseUrl}/reset-password?email={WebUtility.UrlEncode(to)}&token={WebUtility.UrlEncode(token)}";
        
        var htmlContent = $@"
        <html>
        <body>
            <h1>Réinitialisation de mot de passe - ModHub</h1>
            <p>Bonjour {WebUtility.HtmlEncode(username)},</p>
            <p>Vous avez demandé la réinitialisation de votre mot de passe sur ModHub. Veuillez cliquer sur le lien ci-dessous pour définir un nouveau mot de passe :</p>
            <p><a href='{resetLink}'>Réinitialiser mon mot de passe</a></p>
            <p>Ce lien est valable pendant 24 heures.</p>
            <p>Si vous n'avez pas demandé cette réinitialisation, vous pouvez ignorer cet email.</p>
            <p>Cordialement,<br>L'équipe ModHub</p>
        </body>
        </html>";
        
        return await SendEmailAsync(to, "ModHub - Réinitialisation de votre mot de passe", htmlContent);
    }
}
