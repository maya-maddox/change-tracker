using ChangeTracker.Api.Middleware;
using ChangeTracker.Application.Interfaces;
using ChangeTracker.Application.Services;
using ChangeTracker.Infrastructure.Data;
using ChangeTracker.Infrastructure.Data.Repositories;
using ChangeTracker.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    builder.Services.AddDbContext<ChangeTrackerDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IAuditRepository, AuditRepository>();

    builder.Services.AddSingleton<IMessagePublisher>(sp =>
    {
        var connectionString = builder.Configuration.GetConnectionString("RabbitMq")
            ?? "amqp://guest:guest@localhost:5672";
        return RabbitMqPublisher.CreateAsync(connectionString).GetAwaiter().GetResult();
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ChangeTrackerDbContext>();
        db.Database.Migrate();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
