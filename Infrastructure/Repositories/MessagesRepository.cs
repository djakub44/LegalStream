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
        Task CreateMessage(Message message);
    }
    public class MessagesRepository : IMessagesRepository
    {
        private readonly MessageDbContext _context;
        public MessagesRepository(MessageDbContext context)
        {
            _context = context;
        }   
        public async Task CreateMessage(Message message)
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
            _context.Messages.AsNoTracking();
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                throw new KeyNotFoundException($"Message with ID {id} not found.");
            }
            return message;
        }

        public async Task<IEnumerable<Message>> GetMessages()
        {
            _context.Messages.AsNoTracking();
            var messages = await _context.Messages.ToListAsync();
            return messages;
        }
    }
}
