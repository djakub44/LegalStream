using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using Npgsql.EntityFrameworkCore.PostgreSQL;
namespace Infrastructure.Repositories
{
    public interface IOutboxRepository
    {
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task<IEnumerable<OutboxRequest>> GetUnpublishedOutboxRequestsWithLock();
        Task AddOutboxRequestsAsync(IEnumerable<OutboxRequest> outboxRequests);
        Task MarkPublishedAsync(IEnumerable<OutboxRequest> outboxRequests);
    }
    public class OutboxRepository : IOutboxRepository, IDisposable
    {
        private readonly MessagesDbContext _context;
        private readonly ILogger<OutboxRepository> _logger;
        public OutboxRepository(MessagesDbContext context, ILogger<OutboxRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public Task BeginTransactionAsync()
        {
            return _context.Database.BeginTransactionAsync();
        }
        public Task CommitTransactionAsync()
        {
            return _context.Database.CommitTransactionAsync();
        }
        public async Task AddOutboxRequestsAsync(IEnumerable<OutboxRequest> outboxRequests)
        {
            try
            {
                _context.OutboxRequests.AddRange(outboxRequests);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex && pex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogInformation("Duplicate outbox request skipped");
            }
        }
        public async Task<IEnumerable<OutboxRequest>> GetUnpublishedOutboxRequestsWithLock()
        {
            var outboxRequests = await _context.OutboxRequests
                .FromSqlRaw("SELECT * FROM \"OutboxRequests\" WHERE \"PublishedAt\" IS NULL ORDER BY \"CreatedAt\" LIMIT 500 FOR UPDATE SKIP LOCKED")
                .ToListAsync();
            return outboxRequests;
        }
        public async Task MarkPublishedAsync(IEnumerable<OutboxRequest> outboxRequests)
        {
            var publishedAt = DateTime.UtcNow;
            await _context.OutboxRequests.
                Where(r => outboxRequests.Select(or => or.Id).Contains(r.Id)).
                ExecuteUpdateAsync(setters => setters.SetProperty(m => m.PublishedAt, publishedAt));
        }

        public void Dispose()
        {
            _context.Database.CurrentTransaction?.Dispose();
            _context?.Dispose();
        }
    }
}
