using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public interface IMessagesRepository
    {
        Task<Message> GetMessageById(Guid id);
        Task<IEnumerable<Message>> GetMessages();
        Task AddMessage(Message message);
    }
    public class MessagesRepository : IMessagesRepository
    {
        private readonly MessagesDbContext _context;
        private readonly ILogger<MessagesRepository> _logger;
        public MessagesRepository(MessagesDbContext context, ILogger<MessagesRepository> logger)
        {
            _context = context;
            _logger = logger;
        }   
        public async Task AddMessage(Message message)
        {
            try
            {
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pex && pex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                _logger.LogInformation("A message with ID {messageId} already exists.", message.Id);
            }
        }
            
        public async Task<Message> GetMessageById(Guid id)
        {
            var message = await _context.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id) ?? 
                throw new KeyNotFoundException($"Message with ID {id} not found.");
            return message;
        }

        public async Task<IEnumerable<Message>> GetMessages()
        {
            var messages = await _context.Messages.AsNoTracking().ToListAsync();
            return messages;
        }
    }
}
