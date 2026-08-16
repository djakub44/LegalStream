using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public interface IOutboxRepository
    {
        Task<OutboxRequest> GetOutboxRequestByIdAsync(Guid id);
        Task<IEnumerable<OutboxRequest>> GetOutboxRequestsAsync();
        Task<IEnumerable<OutboxRequest>> GetUnpublishedOutboxRequestsAsync();
        Task AddOutboxRequestAsync(OutboxRequest outboxRequest);
        Task DeleteOutboxRequestAsync(Guid id);
        Task UpdateOutboxRequestAsync(Guid id);
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

        public async Task AddOutboxRequestAsync(OutboxRequest outboxRequest)
        {
            try
            {
                _context.OutboxRequests.Add(outboxRequest);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex && pex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogInformation("An outbox request with ID {outboxRequestId} already exists.", outboxRequest.Id);
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
        public async Task UpdateOutboxRequestAsync(Guid id)
        {
            var outboxRequest = await _context.OutboxRequests.FindAsync(id);
            if (outboxRequest != null)
            {
                outboxRequest.PublishedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
