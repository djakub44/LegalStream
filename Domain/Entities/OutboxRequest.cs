using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class OutboxRequest
    {
        public required Guid Id { get; set; }
        public required Message Message { get; set; }
    }
}
