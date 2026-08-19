using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;  
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Npgsql.EntityFrameworkCore.PostgreSQL;

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
                try
                {
                    var delay = 1000;

                    var scopeRead = _scopeFactory.CreateScope();
                    IEnumerable<OutboxRequest> requests = null!;
                    using (scopeRead)
                    {
                        var repository = scopeRead.ServiceProvider.GetRequiredService<IOutboxRepository>();
                        await repository.BeginTransactionAsync(stoppingToken);
                        try
                        {
                            requests = await repository.GetUnpublishedOutboxRequestsWithLock(stoppingToken);

                            if (requests.Any())
                                delay = 0;

                            foreach (var request in requests)
                            {
                                await channel.BasicPublishAsync(
                                    exchange: string.Empty,
                                    routingKey: "messages",
                                    body: Encoding.UTF8.GetBytes(request.Payload));
                            }

                            await repository.MarkPublishedAsync(requests, stoppingToken);

                            await repository.CommitTransactionAsync(stoppingToken);

                        }
                        catch
                        {
                            await repository.RollbackTransactionAsync(CancellationToken.None);
                            throw;
                        }
                    }
                    await Task.Delay(delay, stoppingToken);
                }
                catch(OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    logger.LogInformation("Worker is stopping gracefully.");
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "An error occurred while processing outbox requests.");
                    await Task.Delay(5000, stoppingToken);
                }
                
            }
        }
    }
}
