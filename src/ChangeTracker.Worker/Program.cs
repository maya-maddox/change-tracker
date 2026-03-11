using ChangeTracker.Infrastructure.Data;
using ChangeTracker.Worker;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((_, configuration) =>
        configuration.ReadFrom.Configuration(builder.Configuration));

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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
