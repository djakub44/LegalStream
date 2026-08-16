using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
using Infrastructure.Repositories;
using WebApi.DTO;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly IMessagesRepository _messagesRepository;
        private readonly IOutboxRepository _outboxRepository;

        public MessageController(IMessagesRepository messagesRepository, IOutboxRepository outboxRepository)
        {
            _messagesRepository = messagesRepository;
            _outboxRepository = outboxRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            var messages = await _messagesRepository.GetMessagesAsync();
            return Ok(messages);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetMessage(Guid id)
        {
            var message = await _messagesRepository.GetMessageByIdAsync(id);
            return Ok(message);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest messageRequest)
        {
            var outboxRequest = OutboxRequestMappings.CreateOutboxRequest(messageRequest);
            await _outboxRepository.AddOutboxRequestAsync(outboxRequest);
            return Accepted(outboxRequest);
        }
    }
}