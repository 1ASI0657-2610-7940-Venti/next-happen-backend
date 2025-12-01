using Microsoft.EntityFrameworkCore;
using nexthappen_backend.Metrics.Domain;
using nexthappen_backend.Metrics.Domain.Entities;
using nexthappen_backend.Shared.Infrastructure.Persistence.EFC.Configuration;

public class MetricRepository : IMetricRepository
{
    private readonly AppDbContext _context;

    public MetricRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Metric metric)
    {
        await _context.Set<Metric>().AddAsync(metric);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Metric>> GetAllAsync()
    {
        return await _context.Set<Metric>().ToListAsync();
    }
}