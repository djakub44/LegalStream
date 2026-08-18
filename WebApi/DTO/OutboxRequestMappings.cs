using Domain.Entities;
using Microsoft.AspNetCore.Http.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebApi.DTO;

namespace WebApi.DTO
{
    
    public static class OutboxRequestMappings
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        public static IEnumerable<OutboxRequest> CreateOutboxRequests(IEnumerable<CreateMessageRequest> requests)
        {
            return requests.Select(request => new OutboxRequest
            {
                Id = Guid.NewGuid(),
                Payload = JsonSerializer.Serialize(MessageMappingsWebApi.ToMessage(request), _jsonOptions),
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
