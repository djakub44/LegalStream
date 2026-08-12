using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        public MessagesRepository(MessagesDbContext context)
        {
            _context = context;
        }   
        public async Task AddMessage(Message message)
        {
            var existingMessage = await _context.Messages.FindAsync(message.Id);
            if (existingMessage == null)
            {
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
            }
        }
            
        public async Task<Message> GetMessageById(Guid id)
        {
            var message = await _context.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                throw new KeyNotFoundException($"Message with ID {id} not found.");
            }
            return message;
        }

        public async Task<IEnumerable<Message>> GetMessages()
        {
            var messages = await _context.Messages.AsNoTracking().ToListAsync();
            return messages;
        }
    }
}
