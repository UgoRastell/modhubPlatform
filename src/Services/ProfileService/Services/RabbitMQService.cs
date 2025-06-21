using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProfileService.Settings;

namespace ProfileService.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQService> _logger;
        private readonly Dictionary<string, IModel> _consumerChannels;

        public RabbitMQService(RabbitMQSettings settings, ILogger<RabbitMQService> logger = null)
        {
            _settings = settings;
            _logger = logger;
            _consumerChannels = new Dictionary<string, IModel>();

            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,
                DispatchConsumersAsync = true // Enable async consumers for better performance
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                
                // Declare exchanges that we'll use
                _channel.ExchangeDeclare(settings.ProfileExchange, ExchangeType.Topic, durable: true);
                _channel.ExchangeDeclare(settings.UserExchange, ExchangeType.Topic, durable: true);
                
                _logger?.LogInformation("RabbitMQ connection established to {host}:{port}", settings.HostName, settings.Port);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to establish RabbitMQ connection to {host}:{port}", settings.HostName, settings.Port);
                throw;
            }
        }

        public async Task PublishAsync<T>(string exchange, T message) where T : class
        {
            try
            {
                var routingKey = DeriveRoutingKeyFromType<T>();
                var messageJson = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(messageJson);

                _channel.BasicPublish(
                    exchange: exchange,
                    routingKey: routingKey,
                    basicProperties: null,
                    body: body);

                _logger?.LogInformation("Published message to {exchange} with routing key {routingKey}: {message}", 
                    exchange, routingKey, messageJson);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to publish message to {exchange}", exchange);
                throw;
            }
        }

        public void Subscribe<T>(string exchange, string routingKey, string queueName, Func<T, Task> handler) where T : class
        {
            try
            {
                // Create a dedicated channel for this consumer
                var consumerChannel = _connection.CreateModel();
                _consumerChannels[queueName] = consumerChannel;
                
                // Declare the queue and bind it to the exchange
                consumerChannel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
                consumerChannel.QueueBind(queueName, exchange, routingKey);
                
                // Set prefetch count to control how many messages can be processed simultaneously
                consumerChannel.BasicQos(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(consumerChannel);
                
                consumer.Received += async (sender, args) =>
                {
                    var body = args.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    try
                    {
                        _logger?.LogDebug("Received message from {exchange}/{routingKey}: {message}", exchange, routingKey, message);
                        
                        var deserializedMessage = JsonSerializer.Deserialize<T>(message);
                        
                        // Process the message
                        await handler(deserializedMessage);
                        
                        // Acknowledge the message was processed successfully
                        consumerChannel.BasicAck(args.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing message: {message}", message);
                        
                        // Reject the message and requeue it
                        consumerChannel.BasicNack(args.DeliveryTag, false, true);
                    }
                };
                
                consumerChannel.BasicConsume(queueName, false, consumer);
                
                _logger?.LogInformation("Subscribed to {exchange} with routing key {routingKey} on queue {queueName}", 
                    exchange, routingKey, queueName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to subscribe to {exchange} with routing key {routingKey}", exchange, routingKey);
                throw;
            }
        }

        private string DeriveRoutingKeyFromType<T>()
        {
            var type = typeof(T);
            var typeName = type.Name.ToLowerInvariant();
            
            // Extract routing key from event type name
            if (typeName.EndsWith("event"))
            {
                typeName = typeName.Substring(0, typeName.Length - 5);
            }
            
            // Convert PascalCase to dot.notation
            var routingKey = string.Concat(typeName.Select((x, i) => i > 0 && char.IsUpper(x) ? "." + char.ToLowerInvariant(x) : x.ToString().ToLowerInvariant()));
            
            return routingKey;
        }

        public void Dispose()
        {
            foreach (var channel in _consumerChannels.Values)
            {
                try
                {
                    if (channel.IsOpen)
                        channel.Close();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error closing consumer channel");
                }
            }
            
            try
            {
                if (_channel != null && _channel.IsOpen)
                    _channel.Close();
                
                if (_connection != null && _connection.IsOpen)
                    _connection.Close();
                
                _logger?.LogInformation("RabbitMQ connections closed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing RabbitMQ service");
            }
        }
    }
}
