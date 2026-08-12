using Infrastructure.Repositories;
using MqWorker.DTO;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace MqWorker
{
    
    public class Worker(ILogger<Worker> logger, IServiceScopeFactory _scopeFactory) : BackgroundService
    {
        private readonly ConnectionFactory _factory = new ConnectionFactory();
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            
            _factory.UserName = "guest";
            _factory.Password = "guest";
            _factory.VirtualHost = "/";
            _factory.HostName = "localhost";
            _factory.Port = 5672;

            IConnection conn = await _factory.CreateConnectionAsync();
            IChannel channel = await conn.CreateChannelAsync();

            var messagesQueue = await channel.QueueDeclareAsync("messages", durable: true, exclusive: false, autoDelete: false  );

            var consumer = new AsyncEventingBasicConsumer(channel);
            
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var rawMessage = System.Text.Encoding.UTF8.GetString(body);

                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter());
                var messageReceived = JsonSerializer.Deserialize<MessageReceived>(rawMessage, options);

                if (messageReceived == null)
                {
                    //here will be dead letter logic
                    logger.LogWarning("Received message could not be deserialized: {rawMessage}", rawMessage);
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
                    
                await Task.Yield();
            };
            
            await channel.BasicConsumeAsync("messages", true, consumer);
            
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
