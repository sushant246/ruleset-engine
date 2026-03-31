using RulesetEngine.Domain.Entities;

namespace RulesetEngine.Domain.Interfaces;

public interface IRulesetRepository
{
    Task<IEnumerable<Ruleset>> GetActiveRulesetsAsync();
    Task<Ruleset?> GetByIdAsync(int id);
    Task<Ruleset> AddAsync(Ruleset ruleset);
    Task UpdateAsync(Ruleset ruleset);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}
