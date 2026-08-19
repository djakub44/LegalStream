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
        Task BeginTransactionAsync(CancellationToken cancellationToken);
        Task CommitTransactionAsync(CancellationToken cancellationToken);
        Task RollbackTransactionAsync(CancellationToken cancellationToken);
        Task AddOutboxRequestsAsync(IEnumerable<OutboxRequest> outboxRequests, CancellationToken cancellationToken);
        Task<IEnumerable<OutboxRequest>> GetUnpublishedOutboxRequestsWithLock(CancellationToken cancellationToken);
        Task MarkPublishedAsync(IEnumerable<OutboxRequest> outboxRequests, CancellationToken cancellationToken);
    }
    public class OutboxRepository : IOutboxRepository
    {
        private readonly MessagesDbContext _context;
        private readonly ILogger<OutboxRepository> _logger;
        public OutboxRepository(MessagesDbContext context, ILogger<OutboxRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public Task BeginTransactionAsync(CancellationToken cancellationToken)
        {
            return _context.Database.BeginTransactionAsync(cancellationToken);
        }
        public Task CommitTransactionAsync(CancellationToken cancellationToken)
        {
            return _context.Database.CommitTransactionAsync(cancellationToken);
        }
        public Task RollbackTransactionAsync(CancellationToken cancellationToken)
        {
            return _context.Database.RollbackTransactionAsync(cancellationToken);
        }
        public async Task AddOutboxRequestsAsync(IEnumerable<OutboxRequest> outboxRequests, CancellationToken cancellationToken)
        {
            try
            {
                _context.OutboxRequests.AddRange(outboxRequests);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex && pex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogInformation("Duplicate outbox request skipped");
            }
        }
        public async Task<IEnumerable<OutboxRequest>> GetUnpublishedOutboxRequestsWithLock(CancellationToken cancellationToken)
        {
            var outboxRequests = await _context.OutboxRequests
                .FromSqlRaw("SELECT * FROM \"OutboxRequests\" WHERE \"PublishedAt\" IS NULL ORDER BY \"CreatedAt\" LIMIT 500 FOR UPDATE SKIP LOCKED")
                .ToListAsync(cancellationToken);
            return outboxRequests;
        }
        public async Task MarkPublishedAsync(IEnumerable<OutboxRequest> outboxRequests, CancellationToken cancellationToken)
        {
            var publishedAt = DateTime.UtcNow;
            await _context.OutboxRequests.
                Where(r => outboxRequests.Select(or => or.Id).Contains(r.Id)).
                ExecuteUpdateAsync(setters => setters.SetProperty(m => m.PublishedAt, publishedAt), cancellationToken);
        }
    }
}
    