namespace ProfileService.Settings
{
    public class RabbitMQSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        
        // Exchange and queue names for events relevant to profile service
        public string ProfileExchange { get; set; } = "profile.events";
        public string UserExchange { get; set; } = "user.events";
        public string ProfileQueue { get; set; } = "profile.queue";
        public string UserCreatedRoutingKey { get; set; } = "user.created";
        public string UserDeletedRoutingKey { get; set; } = "user.deleted";
    }
}
