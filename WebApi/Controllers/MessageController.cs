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

        public MessageController(IMessagesRepository messagesRepository)
        {
            _messagesRepository = messagesRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            var messages = await _messagesRepository.GetMessages();
            return Ok(messages);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetMessage(Guid id)
        {
            var message = await _messagesRepository.GetMessageById(id);
            return Ok(message);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest messageRequest)
        {
            var message = MessageMappingsWebApi.ToMessage(messageRequest);
            await _messagesRepository.AddMessage(message);
            return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, message);
        }
    }
}