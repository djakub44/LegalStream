using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;  
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace MqRelayWorker
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

            while (!stoppingToken.IsCancellationRequested)
            {
                var scopeRead = _scopeFactory.CreateScope();
                IEnumerable<OutboxRequest> requests = null!;
                using (scopeRead)
                {
                    var repository = scopeRead.ServiceProvider.GetRequiredService<IOutboxRepository>();
                    requests = await repository.GetUnpublishedOutboxRequestsAsync();
                }

                foreach(var request in requests)
                {
                    await channel.BasicPublishAsync(
                        exchange: string.Empty,
                        routingKey: "messages", 
                        body: Encoding.UTF8.GetBytes(request.Payload));
                    
                    var scopeSave = _scopeFactory.CreateScope();
                    using (scopeSave)
                    {
                        var repository = scopeSave.ServiceProvider.GetRequiredService<IOutboxRepository>();
                        request.PublishedAt = DateTime.UtcNow;
                        await repository.UpdateOutboxRequestAsync(request.Id);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
