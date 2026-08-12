using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MqWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            //builder.Services.AddDbContext<MessagesDbContext>(options =>
            //    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
            //builder.Services.AddScoped<IMessagesRepository, MessagesRepository>();
            //replaced by:
            builder.Services.AddInfrastructure(builder.Configuration);

            var host = builder.Build();
            host.Run();
        }
    }
}
