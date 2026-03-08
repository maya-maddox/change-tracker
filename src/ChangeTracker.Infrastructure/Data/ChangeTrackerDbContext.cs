using ChangeTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChangeTracker.Infrastructure.Data;

public class ChangeTrackerDbContext(DbContextOptions<ChangeTrackerDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChangeTrackerDbContext).Assembly);
    }
}
