using ChangeTracker.Application.Interfaces;
using ChangeTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChangeTracker.Infrastructure.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ChangeTrackerDbContext _context;

    public ProductRepository(ChangeTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync([id], cancellationToken);
        if (product is not null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
