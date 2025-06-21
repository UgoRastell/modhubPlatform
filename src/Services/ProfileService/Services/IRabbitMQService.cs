namespace ProfileService.Services
{
    public interface IRabbitMQService
    {
        Task PublishAsync<T>(string exchange, T message) where T : class;
        void Subscribe<T>(string exchange, string routingKey, string queueName, Func<T, Task> handler) where T : class;
    }
}
