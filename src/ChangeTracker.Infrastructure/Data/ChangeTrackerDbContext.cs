using ChangeTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChangeTracker.Infrastructure.Data;

public class ChangeTrackerDbContext : DbContext
{
    public ChangeTrackerDbContext(DbContextOptions<ChangeTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChangeTrackerDbContext).Assembly);
    }
}
