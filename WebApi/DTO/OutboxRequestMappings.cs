using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using WebApi.DTO;

namespace WebApi.DTO
{
    public static class OutboxRequestMappings
    {
        public static OutboxRequest CreateOutboxRequest(CreateMessageRequest request)
        {
            return new OutboxRequest
            {
                Id = Guid.NewGuid(),
                Message = MessageMappingsWebApi.ToMessage(request)
            };
        }
    }
}
