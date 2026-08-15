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
        Task<OutboxRequest> GetOutboxRequestById(Guid id);
        Task<IEnumerable<OutboxRequest>> GetOutboxRequests();
        Task AddOutboxRequest(OutboxRequest outboxRequest);
        Task DeleteOutboxRequest(Guid id);
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

        public async Task AddOutboxRequest(OutboxRequest outboxRequest)
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

        public async Task DeleteOutboxRequest(Guid id)
        {
            var outboxRequest = await _context.OutboxRequests.FindAsync(id);
            if (outboxRequest != null)
            {
                _context.OutboxRequests.Remove(outboxRequest);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<OutboxRequest> GetOutboxRequestById(Guid id)
        {
            var outboxRequest = await _context.OutboxRequests.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id) ??
                throw new KeyNotFoundException($"Outbox request with ID {id} not found.");
            return outboxRequest;
        }
        

        public async Task<IEnumerable<OutboxRequest>> GetOutboxRequests()
        {
            var outboxRequests = await _context.OutboxRequests.AsNoTracking().ToListAsync();
            return outboxRequests;
        }
    }
}
