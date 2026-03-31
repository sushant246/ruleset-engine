using Microsoft.EntityFrameworkCore;
using RulesetEngine.Domain.Entities;
using RulesetEngine.Domain.Interfaces;
using RulesetEngine.Infrastructure.Database;

namespace RulesetEngine.Infrastructure.Repositories;

public class RulesetRepository : IRulesetRepository
{
    private readonly RulesetDbContext _context;

    public RulesetRepository(RulesetDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Ruleset>> GetActiveRulesetsAsync()
    {
        return await _context.Rulesets
            .Where(r => r.IsActive)
            .Include(r => r.Conditions)
            .Include(r => r.Rules)
                .ThenInclude(rule => rule.Conditions)
            .Include(r => r.Rules)
                .ThenInclude(rule => rule.Result)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }

    public async Task<Ruleset?> GetByIdAsync(int id)
    {
        return await _context.Rulesets
            .Include(r => r.Conditions)
            .Include(r => r.Rules)
                .ThenInclude(rule => rule.Conditions)
            .Include(r => r.Rules)
                .ThenInclude(rule => rule.Result)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Ruleset> AddAsync(Ruleset ruleset)
    {
        _context.Rulesets.Add(ruleset);
        await _context.SaveChangesAsync();
        return ruleset;
    }

    public async Task UpdateAsync(Ruleset ruleset)
    {
        ruleset.UpdatedAt = DateTime.UtcNow;
        _context.Rulesets.Update(ruleset);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var ruleset = await _context.Rulesets.FindAsync(id);
        if (ruleset != null)
        {
            _context.Rulesets.Remove(ruleset);
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
