using Infrastructure.Repositories;
using MqConsumerWorker.DTO;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace MqConsumerWorker
{
    
    public class Worker(ILogger<Worker> logger, IServiceScopeFactory _scopeFactory) : BackgroundService
    {
        private readonly ConnectionFactory _factory = new ConnectionFactory();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            _factory.UserName = "guest";
            _factory.Password = "guest";
            _factory.VirtualHost = "/";
            _factory.HostName = "localhost";
            _factory.Port = 5672;

            IConnection conn = await _factory.CreateConnectionAsync();
            IChannel channel = await conn.CreateChannelAsync();

            await channel.ExchangeDeclareAsync("messages.dlx", ExchangeType.Fanout, durable: true);
            await channel.QueueDeclareAsync("messages.dead", durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync("messages.dead", "messages.dlx", routingKey: "");

            var args = new Dictionary<string, object?> { { "x-dead-letter-exchange", "messages.dlx" } };

            await channel.QueueDeclareAsync("messages", durable: true, exclusive: false, autoDelete: false, arguments: args);

            var consumer = new AsyncEventingBasicConsumer(channel);
            
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var rawMessage = System.Text.Encoding.UTF8.GetString(body);
                MessageReceived? messageReceived = null;
                try
                {
                    messageReceived = JsonSerializer.Deserialize<MessageReceived>(rawMessage, _jsonOptions);
                }
                catch(JsonException ex)
                {
                    logger.LogError(ex, "Error deserializing message: {rawMessage}", rawMessage);
                    
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }
                if (messageReceived == null)
                {
                    logger.LogWarning("Received message is null: {rawMessage}", rawMessage);
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }
                
                logger.LogInformation("Raw message received : {rawMessage}", rawMessage);

                var scope = _scopeFactory.CreateScope();
                using (scope)
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IMessagesRepository>();
                    var messageEntity = MessageMappingsMq.ToMessage(messageReceived);
                    await repository.AddMessage(messageEntity);
                }
                
                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            };


            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
            await channel.BasicConsumeAsync(queue: "messages", autoAck: false, consumer: consumer);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {

                    // Log that the worker is running
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
