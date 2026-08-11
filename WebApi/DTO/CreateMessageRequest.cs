using Domain.Entities;

namespace WebApi.DTO
{
    public class CreateMessageRequest
    {
        public required MessageType Type { get; set; }
        public required string Payload { get; set; }
    }

    public static class MessageMappings
    {
        public static Message ToMessage(CreateMessageRequest request)
        {
            return new Message
            {
                Id = Guid.NewGuid(),
                Type = request.Type,
                Payload = request.Payload,
                ReceivedAt = DateTime.UtcNow
            };
        }
    }
}
