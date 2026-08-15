using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class OutboxRequest
    {
        public required Guid Id { get; set; }
        public required string Payload { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
