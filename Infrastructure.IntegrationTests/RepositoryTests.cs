using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Domain.Entities;
using System.Linq;

namespace Infrastructure.IntegrationTests
{
    
    public class RepositoryTests : IAsyncLifetime 
    {
        private PostgreSqlContainer _postgres;
        private DbContextOptions<MessagesDbContext> _dbContextOptions;
        public async ValueTask InitializeAsync()
        {
            _postgres = new PostgreSqlBuilder("postgres:17").Build();
            await _postgres.StartAsync();

            var connectionString = _postgres.GetConnectionString();
            _dbContextOptions = new DbContextOptionsBuilder<MessagesDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            await using var context = new MessagesDbContext(_dbContextOptions);
            await context.Database.MigrateAsync(); //this ensures 1:1 prod schema
        }

        public async ValueTask DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        [Fact]
        public async Task MessageSavedTwiceToDatabaseOnlyExistsOnce()
        {
            //Arrange
            var logger = NullLogger<MessagesRepository>.Instance;
            
            var message = new Message()
            {
                Id = Guid.NewGuid(),
                Payload = "Test Payload",
                Type = MessageType.Swift,
                ReceivedAt = DateTime.UtcNow
            };
            
            //Act
            var repository = new MessagesRepository(new MessagesDbContext(_dbContextOptions), logger);
            await repository.AddMessageAsync(message);

            repository = new MessagesRepository(new MessagesDbContext(_dbContextOptions), logger);
            await repository.AddMessageAsync(message);

            //Assert
            repository = new MessagesRepository(new MessagesDbContext(_dbContextOptions), logger);
            var retrievedMessages = await repository.GetMessagesAsync();
            Assert.True(retrievedMessages.Count() == 1);
            Assert.True(retrievedMessages.First().Payload == message.Payload);
            Assert.True(retrievedMessages.First().Id == message.Id);

        }
    }


        
    }
