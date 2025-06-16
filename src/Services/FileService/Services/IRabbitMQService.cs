namespace FileService.Services
{
    public interface IRabbitMQService
    {
        /// <summary>
        /// Publishes an event to the specified RabbitMQ exchange
        /// </summary>
        /// <param name="exchange">Exchange name</param>
        /// <param name="eventData">Event data to be published</param>
        /// <param name="routingKey">Optional routing key (defaults to event type name)</param>
        Task PublishAsync(string exchange, object eventData, string routingKey = null);
        
        /// <summary>
        /// Subscribes to events from the specified exchange and queue
        /// </summary>
        /// <param name="exchange">Exchange name</param>
        /// <param name="queue">Queue name</param>
        /// <param name="routingKey">Routing key pattern</param>
        /// <param name="handler">Handler function that processes the message</param>
        /// <param name="eventType">Type of event to deserialize to</param>
        void Subscribe(string exchange, string queue, string routingKey, Func<object, Task> handler, Type eventType);
        
        /// <summary>
        /// Subscribes to events of a specific type
        /// </summary>
        /// <typeparam name="T">Type of event to subscribe to</typeparam>
        /// <param name="exchange">Exchange name</param>
        /// <param name="queue">Queue name</param>
        /// <param name="routingKey">Routing key pattern</param>
        /// <param name="handler">Handler function that processes the event</param>
        void Subscribe<T>(string exchange, string queue, string routingKey, Func<T, Task> handler);
        
        /// <summary>
        /// Creates the necessary exchanges, queues, and bindings
        /// </summary>
        void EnsureInitialized();
    }
}
