using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MqWorker.DTO
{
    public class MessageReceived
    {
        public required MessageType Type { get; set; }
        public required string Payload { get; set; }
        public required Guid Id { get; set; }
    }

    public static class MessageMappingsMq
    {
        public static Message ToMessage(MessageReceived request)
        {
            return new Message
            {
                Id = request.Id,
                Type = request.Type,
                Payload = request.Payload,
                ReceivedAt = DateTime.UtcNow
            };
        }
    }
}
