using RulesetEngine.Domain.Entities;

namespace RulesetEngine.Domain.Interfaces;

public interface IEvaluationLogRepository
{
    Task<EvaluationLog> AddAsync(EvaluationLog log);
    Task<IEnumerable<EvaluationLog>> GetByOrderIdAsync(string orderId);
    Task SaveChangesAsync();
}
