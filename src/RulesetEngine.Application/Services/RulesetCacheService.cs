using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RulesetEngine.Domain.Entities;
using RulesetEngine.Domain.Interfaces;

namespace RulesetEngine.Application.Services;

/// <summary>
/// Service for caching active rulesets to avoid repeated database queries during evaluations.
/// Cache is invalidated when rulesets are created, updated, or deleted.
/// </summary>
public interface IRulesetCacheService
{
    Task<IEnumerable<Ruleset>> GetActiveRulesetsAsync(IRulesetRepository repository);
    void InvalidateCache();
}

public class RulesetCacheService : IRulesetCacheService
{
    private const string CacheKey = "active_rulesets";
    private const int CacheDurationMinutes = 60;
    
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RulesetCacheService> _logger;

    public RulesetCacheService(IMemoryCache memoryCache, ILogger<RulesetCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<IEnumerable<Ruleset>> GetActiveRulesetsAsync(IRulesetRepository repository)
    {
        if (_memoryCache.TryGetValue(CacheKey, out IEnumerable<Ruleset>? cachedRulesets))
        {
            _logger.LogDebug("Cache hit: Retrieved active rulesets from cache");
            return cachedRulesets ?? Enumerable.Empty<Ruleset>();
        }

        _logger.LogDebug("Cache miss: Loading active rulesets from database");
        var rulesets = (await repository.GetActiveRulesetsAsync()).ToList();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheDurationMinutes))
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));

        _memoryCache.Set(CacheKey, rulesets, cacheOptions);
        _logger.LogDebug("Cached {RulesetCount} active rulesets for {CacheDurationMinutes} minutes", 
            rulesets.Count, CacheDurationMinutes);

        return rulesets;
    }

    public void InvalidateCache()
    {
        _memoryCache.Remove(CacheKey);
        _logger.LogInformation("Ruleset cache invalidated");
    }
}
