using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentsService.Config;
using PaymentsService.Services;

namespace PaymentsService.Services
{
    /// <summary>
    /// Configuration pour le service d'email
    /// </summary>
    public class EmailOptions
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }

    /// <summary>
    /// Service d'envoi d'emails pour les notifications liées aux paiements
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailOptions _options;
        private readonly IUserService _userService;

        public EmailService(
            ILogger<EmailService> logger,
            IOptions<EmailOptions> options,
            IUserService userService)
        {
            _logger = logger;
            _options = options.Value;
            _userService = userService;
        }

        /// <inheritdoc />
        public async Task<bool> SendTransactionNotificationAsync(string userId, string subject, string message)
        {
            try
            {
                // Récupérer l'email de l'utilisateur
                var userEmail = await _userService.GetUserEmailAsync(userId);
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Impossible d'envoyer une notification à l'utilisateur {UserId}: email non trouvé", userId);
                    return false;
                }

                return await SendEmailAsync(userEmail, subject, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de la notification à l'utilisateur {UserId}: {Message}", userId, ex.Message);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SendAdminAlertAsync(string subject, string message, int priority = 3)
        {
            try
            {
                // Ajouter le niveau de priorité au sujet
                string prioritySubject = $"[PRIORITÉ {priority}/5] {subject}";
                
                return await SendEmailAsync(_options.AdminEmail, prioritySubject, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'alerte aux administrateurs: {Message}", ex.Message);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SendTransactionSummaryAsync(string userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Récupérer l'email de l'utilisateur
                var userEmail = await _userService.GetUserEmailAsync(userId);
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("Impossible d'envoyer un récapitulatif à l'utilisateur {UserId}: email non trouvé", userId);
                    return false;
                }

                var subject = $"Récapitulatif de vos transactions du {startDate:dd/MM/yyyy} au {endDate:dd/MM/yyyy}";
                
                // Construire le corps du message avec les transactions de l'utilisateur
                // Note: Dans une implémentation réelle, il faudrait récupérer les transactions de l'utilisateur
                // et générer un rapport détaillé, éventuellement avec un template HTML
                var message = $"Récapitulatif de vos transactions du {startDate:dd/MM/yyyy} au {endDate:dd/MM/yyyy}\n\n" +
                             "Pour consulter le détail de vos transactions, connectez-vous à votre espace personnel.";

                return await SendEmailAsync(userEmail, subject, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi du récapitulatif à l'utilisateur {UserId}: {Message}", userId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Méthode privée pour envoyer un email
        /// </summary>
        private async Task<bool> SendEmailAsync(string toEmail, string subject, string messageBody)
        {
            try
            {
                _logger.LogInformation("Envoi d'un e-mail à {ToEmail} avec le sujet '{Subject}'", toEmail, subject);

                // Vérifier que les paramètres SMTP sont configurés
                if (string.IsNullOrEmpty(_options.SmtpServer) || string.IsNullOrEmpty(_options.FromEmail))
                {
                    _logger.LogWarning("Configuration SMTP incomplète, l'e-mail n'a pas été envoyé");
                    return false;
                }

                // Créer le message
                using var mail = new MailMessage();
                mail.From = new MailAddress(_options.FromEmail, _options.FromName);
                mail.To.Add(toEmail);
                mail.Subject = subject;
                mail.Body = messageBody;
                mail.IsBodyHtml = false; // Peut être mis à true pour des messages HTML

                // Configurer le client SMTP
                using var client = new SmtpClient(_options.SmtpServer, _options.SmtpPort);
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword);
                client.EnableSsl = _options.EnableSsl;

                // Envoyer l'e-mail de manière asynchrone
                await client.SendMailAsync(mail);
                
                _logger.LogInformation("E-mail envoyé avec succès à {ToEmail}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'e-mail à {ToEmail}: {Message}", toEmail, ex.Message);
                return false;
            }
        }
    }
}
