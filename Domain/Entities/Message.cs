using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Message
    {
        public required Guid Id { get; set; }
        public required MessageType Type { get; set; }
        public required string Payload { get; set; }
        public required DateTime ReceivedAt { get; set; }
    }

    public enum MessageType
    {
        Swift,
        XML,
    }
}
