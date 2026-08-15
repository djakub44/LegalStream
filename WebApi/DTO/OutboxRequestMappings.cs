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
        public static OutboxRequest CreateOutboxRequest(CreateMessageRequest request)
        {
            return new OutboxRequest
            {
                Id = Guid.NewGuid(),
                Payload = JsonSerializer.Serialize(request, _jsonOptions),
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
