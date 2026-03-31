using Microsoft.EntityFrameworkCore;
using RulesetEngine.Domain.Entities;
using RulesetEngine.Domain.Interfaces;
using RulesetEngine.Infrastructure.Database;

namespace RulesetEngine.Infrastructure.Repositories;

public class EvaluationLogRepository : IEvaluationLogRepository
{
    private readonly RulesetDbContext _context;

    public EvaluationLogRepository(RulesetDbContext context)
    {
        _context = context;
    }

    public async Task<EvaluationLog> AddAsync(EvaluationLog log)
    {
        _context.EvaluationLogs.Add(log);
        return await Task.FromResult(log);
    }

    public async Task<IEnumerable<EvaluationLog>> GetByOrderIdAsync(string orderId)
    {
        return await _context.EvaluationLogs
            .Where(l => l.OrderId == orderId)
            .OrderByDescending(l => l.EvaluatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<EvaluationLog>> GetRecentAsync(int count = 100)
    {
        return await _context.EvaluationLogs
            .OrderByDescending(l => l.EvaluatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
