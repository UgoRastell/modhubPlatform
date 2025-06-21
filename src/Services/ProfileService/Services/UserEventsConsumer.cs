using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProfileService.Models;
using ProfileService.Settings;
using System.Text.Json;

namespace ProfileService.Services
{
    public class UserEventsConsumer : BackgroundService
    {
        private readonly IRabbitMQService _rabbitMQService;
        private readonly IProfileService _profileService;
        private readonly ILogger<UserEventsConsumer> _logger;
        private readonly RabbitMQSettings _rabbitMQSettings;

        public UserEventsConsumer(
            IRabbitMQService rabbitMQService,
            IProfileService profileService,
            RabbitMQSettings rabbitMQSettings,
            ILogger<UserEventsConsumer> logger)
        {
            _rabbitMQService = rabbitMQService;
            _profileService = profileService;
            _rabbitMQSettings = rabbitMQSettings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("User Events Consumer started at: {time}", DateTimeOffset.Now);

            // Subscribe to UserCreated events
            _rabbitMQService.Subscribe<UserCreatedEvent>(
                _rabbitMQSettings.UserExchange,
                _rabbitMQSettings.UserCreatedRoutingKey,
                _rabbitMQSettings.ProfileQueue + ".user.created",
                async (userCreatedEvent) =>
                {
                    try
                    {
                        _logger.LogInformation("Processing UserCreated event for user {UserId}", userCreatedEvent.UserId);
                        
                        // Create a new profile for the user
                        await _profileService.CreateProfileAsync(userCreatedEvent.UserId, userCreatedEvent.Username);
                        
                        _logger.LogInformation("Successfully created profile for user {UserId}", userCreatedEvent.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing UserCreated event for user {UserId}", userCreatedEvent.UserId);
                    }
                });

            // Subscribe to UserDeleted events
            _rabbitMQService.Subscribe<UserDeletedEvent>(
                _rabbitMQSettings.UserExchange,
                _rabbitMQSettings.UserDeletedRoutingKey,
                _rabbitMQSettings.ProfileQueue + ".user.deleted",
                async (userDeletedEvent) =>
                {
                    try
                    {
                        _logger.LogInformation("Processing UserDeleted event for user {UserId}", userDeletedEvent.UserId);
                        
                        // Delete the user's profile and all related data
                        await _profileService.DeleteProfileAsync(userDeletedEvent.UserId);
                        
                        _logger.LogInformation("Successfully deleted profile for user {UserId}", userDeletedEvent.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing UserDeleted event for user {UserId}", userDeletedEvent.UserId);
                    }
                });

            // Keep the background service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    public class UserCreatedEvent
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserDeletedEvent
    {
        public string UserId { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
