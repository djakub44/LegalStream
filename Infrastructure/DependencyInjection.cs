using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Infrastructure.Repositories;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<MessagesDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("Postgres")));
            services.AddScoped<IMessagesRepository, MessagesRepository>();
            return services;
        }
    }
}
