using ChangeTracker.Infrastructure.Data;
using ChangeTracker.Worker;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ChangeTrackerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnection>(_ =>
{
    var connectionString = builder.Configuration.GetConnectionString("RabbitMq")
        ?? "amqp://guest:guest@localhost:5672";
    var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddHostedService<AuditConsumer>();

var host = builder.Build();
host.Run();
