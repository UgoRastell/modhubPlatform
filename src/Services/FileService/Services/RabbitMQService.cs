using FileService.Settings;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace FileService.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQService> _logger;
        private readonly IConnection _connection;
        private readonly ConcurrentDictionary<string, IModel> _channels = new ConcurrentDictionary<string, IModel>();
        private bool _isInitialized = false;
        private readonly object _initLock = new object();

        public RabbitMQService(RabbitMQSettings settings, ILogger<RabbitMQService> logger)
        {
            _settings = settings;
            _logger = logger;

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = settings.HostName,
                    Port = settings.Port,
                    UserName = settings.UserName,
                    Password = settings.Password,
                    VirtualHost = settings.VirtualHost,
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection(clientProvidedName: "FileService");
                _logger.LogInformation("Connected to RabbitMQ at {hostname}:{port}", settings.HostName, settings.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish connection to RabbitMQ");
                throw;
            }
        }

        public void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_initLock)
            {
                if (_isInitialized)
                {
                    return;
                }

                try
                {
                    using var channel = _connection.CreateModel();

                    // Declare exchanges
                    DeclareExchange(channel, _settings.FileExchange);
                    DeclareExchange(channel, _settings.UserExchange);
                    DeclareExchange(channel, _settings.ModExchange);

                    // Declare queues for file service
                    DeclareQueue(channel, _settings.FileUploadedQueue);
                    DeclareQueue(channel, _settings.FileScannedQueue);
                    DeclareQueue(channel, _settings.FileDeletedQueue);
                    
                    // Queues to receive events from other services
                    DeclareQueue(channel, _settings.UserEventsQueue);
                    DeclareQueue(channel, _settings.ModEventsQueue);

                    // Binding queues to exchanges
                    channel.QueueBind(_settings.UserEventsQueue, _settings.UserExchange, "user.*");
                    channel.QueueBind(_settings.ModEventsQueue, _settings.ModExchange, "mod.*");

                    _isInitialized = true;
                    _logger.LogInformation("RabbitMQ exchanges and queues initialized");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize RabbitMQ exchanges and queues");
                    throw;
                }
            }
        }

        private void DeclareExchange(IModel channel, string exchangeName)
        {
            channel.ExchangeDeclare(
                exchange: exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null
            );
        }

        private void DeclareQueue(IModel channel, string queueName)
        {
            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    { "x-dead-letter-exchange", $"{queueName}.dlx" },
                    { "x-message-ttl", 86400000 } // 24 hours
                }
            );
            
            // Declare dead-letter exchange and queue for this queue
            var dlxName = $"{queueName}.dlx";
            var dlqName = $"{queueName}.dlq";
            
            channel.ExchangeDeclare(dlxName, ExchangeType.Fanout, durable: true);
            channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(dlqName, dlxName, "");
        }

        public async Task PublishAsync(string exchange, object eventData, string routingKey = null)
        {
            try
            {
                EnsureInitialized();

                if (string.IsNullOrEmpty(routingKey))
                {
                    // Default routing key is the event class name without "Event" suffix
                    var typeName = eventData.GetType().Name;
                    routingKey = typeName.EndsWith("Event") ? 
                        typeName.Substring(0, typeName.Length - 5).ToLower() : 
                        typeName.ToLower();
                }

                var messageJson = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var body = Encoding.UTF8.GetBytes(messageJson);

                var channel = GetOrCreateChannel();
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.Type = eventData.GetType().AssemblyQualifiedName;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                channel.BasicPublish(
                    exchange: exchange,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body
                );

                _logger.LogInformation("Published message to {exchange} with routing key {routingKey}", exchange, routingKey);

                // Latency Consideration: PublishAsync should still return quickly after publishing the message
                // We're not actually waiting for any asynchronous operation to complete here
                // but maintaining the async signature for consistency with other service interfaces
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to exchange {exchange}", exchange);
                throw;
            }
        }

        public void Subscribe(string exchange, string queue, string routingKey, Func<object, Task> handler, Type eventType)
        {
            try
            {
                EnsureInitialized();

                var channel = GetOrCreateChannel(queue);
                channel.QueueBind(queue, exchange, routingKey);
                
                var consumer = new AsyncEventingBasicConsumer(channel);
                
                consumer.Received += async (sender, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        var eventObj = JsonSerializer.Deserialize(message, eventType, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (eventObj != null)
                        {
                            await handler(eventObj);
                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from queue {queue}", queue);
                        
                        // Requeue if it's a transient error
                        // Otherwise, message will go to DLQ after retry limit
                        if (ShouldRetry(ex, ea.BasicProperties))
                        {
                            channel.BasicNack(ea.DeliveryTag, false, true);
                        }
                        else
                        {
                            channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    }
                };

                channel.BasicQos(0, 10, false);
                channel.BasicConsume(queue, false, consumer);

                _logger.LogInformation("Subscribed to {exchange}:{queue} with routing key {routingKey}", exchange, queue, routingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to exchange {exchange} on queue {queue}", exchange, queue);
                throw;
            }
        }

        public void Subscribe<T>(string exchange, string queue, string routingKey, Func<T, Task> handler)
        {
            Subscribe(exchange, queue, routingKey, 
                async (object o) => await handler((T)o),
                typeof(T));
        }

        private IModel GetOrCreateChannel(string name = "default")
        {
            return _channels.GetOrAdd(name, _ => 
            {
                var channel = _connection.CreateModel();
                _logger.LogDebug("Created new channel: {name}", name);
                return channel;
            });
        }

        private bool ShouldRetry(Exception ex, IBasicProperties properties)
        {
            // Retry for network-related errors
            if (ex is System.IO.IOException || ex is System.Net.Sockets.SocketException)
                return true;

            // Check if we have retry info in headers
            if (properties.Headers != null && properties.Headers.TryGetValue("x-retry-count", out var retryObj))
            {
                var retryCount = (int)retryObj;
                return retryCount < _settings.MaxRetryCount;
            }

            return true; // Default to retry
        }

        public void Dispose()
        {
            foreach (var channel in _channels.Values)
            {
                try
                {
                    if (channel.IsOpen)
                    {
                        channel.Close();
                    }
                    channel.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing RabbitMQ channel");
                }
            }

            if (_connection?.IsOpen == true)
            {
                try
                {
                    _connection.Close();
                    _connection.Dispose();
                    _logger.LogInformation("RabbitMQ connection closed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing RabbitMQ connection");
                }
            }
        }
    }
}
