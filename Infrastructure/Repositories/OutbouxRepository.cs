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
        Task<OutboxRequest> GetOutboxRequestByIdAsync(Guid id);
        Task<IEnumerable<OutboxRequest>> GetOutboxRequestsAsync();
        Task<IEnumerable<OutboxRequest>> GetUnpublishedOutboxRequestsAsync();
        Task<IEnumerable<OutboxRequest>> GetUnpublishedOutboxRequestsWithLock();
        Task AddOutboxRequestsAsync(IEnumerable<OutboxRequest> outboxRequests);
        Task DeleteOutboxRequestAsync(Guid id);
        Task UpdateOutboxRequestAsync(Guid id);
        Task MarkPublishedAsync(IEnumerable<OutboxRequest> outboxRequests);
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

        public async Task AddOutboxRequestsAsync(IEnumerable<OutboxRequest> outboxRequests)
        {
            try
            {
                _context.OutboxRequests.AddRange(outboxRequests);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex && pex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogInformation("An outbox request with ID {outboxRequestId} already exists.", pex.Line);
            }
        }

        public async Task DeleteOutboxRequestAsync(Guid id)
        {
            var outboxRequest = await _context.OutboxRequests.FindAsync(id);
            if (outboxRequest != null)
            {
                _context.OutboxRequests.Remove(outboxRequest);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<OutboxRequest> GetOutboxRequestByIdAsync(Guid id)
        {
            var outboxRequest = await _context.OutboxRequests.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id) ??
                throw new KeyNotFoundException($"Outbox request with ID {id} not found.");
            return outboxRequest;
        }
        

        public async Task<IEnumerable<OutboxRequest>> GetOutboxRequestsAsync()
        {
            var outboxRequests = await _context.OutboxRequests.AsNoTracking().ToListAsync();
            return outboxRequests;
        }
        public async Task<IEnumerable<OutboxRequest>> GetUnpublishedOutboxRequestsAsync()
        {
            var unpublishedRequests = await _context.OutboxRequests.AsNoTracking()
                .Where(r => r.PublishedAt == null)
                .ToListAsync();
            return unpublishedRequests;
        }
        public async Task<IEnumerable<OutboxRequest>> GetUnpublishedOutboxRequestsWithLock()
        {
            var outboxRequests = await _context.OutboxRequests
                .FromSqlRaw("SELECT * FROM \"OutboxRequests\" WHERE \"PublishedAt\" IS NULL ORDER BY \"CreatedAt\" LIMIT 500 FOR UPDATE SKIP LOCKED")
                .ToListAsync();
            return outboxRequests;
        }
        public async Task UpdateOutboxRequestAsync(Guid id)
        {
            var outboxRequest = await _context.OutboxRequests.FindAsync(id);
            if (outboxRequest != null)
            {
                outboxRequest.PublishedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        public async Task MarkPublishedAsync(IEnumerable<OutboxRequest> outboxRequests)
        {
            var publishedAt = DateTime.UtcNow;
            await _context.OutboxRequests.
                Where(r => outboxRequests.Select(or => or.Id).Contains(r.Id)).
                ExecuteUpdateAsync(setters => setters.SetProperty(m => m.PublishedAt, publishedAt));
        }
    }
}
