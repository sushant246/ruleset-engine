using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RulesetEngine.Application.DTOs;

namespace RulesetEngine.Application.Services;

/// <summary>
/// Tracks evaluation metrics for performance monitoring and analytics
/// </summary>
public interface IEvaluationMetricsService
{
    void RecordEvaluation(EvaluationMetrics metrics);
    EvaluationMetricsSummary GetSummary();
    void Reset();
}

public class EvaluationMetrics
{
    public string? OrderId { get; set; }
    public string? MatchedRuleset { get; set; }
    public string? MatchedRule { get; set; }
    public bool Matched { get; set; }
    public bool FallbackUsed { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public DateTime EvaluatedAt { get; set; }
}

public class EvaluationMetricsSummary
{
    public long TotalEvaluations { get; set; }
    public long SuccessfulMatches { get; set; }
    public long FallbackUsages { get; set; }
    public long NoMatch { get; set; }
    public double MatchRate { get; set; }
    public double FallbackRate { get; set; }
    public double AverageEvaluationTimeMs { get; set; }
    public long MaxEvaluationTimeMs { get; set; }
    public long MinEvaluationTimeMs { get; set; }
    public Dictionary<string, int> RulesetMatchCount { get; set; } = new();
    public Dictionary<string, int> RuleMatchCount { get; set; } = new();
    public DateTime CollectionStartTime { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class EvaluationMetricsService : IEvaluationMetricsService
{
    private readonly ILogger<EvaluationMetricsService> _logger;
    private long _totalEvaluations;
    private long _successfulMatches;
    private long _fallbackUsages;
    private long _noMatch;
    private long _totalEvaluationTimeMs;
    private long _maxEvaluationTimeMs;
    private long _minEvaluationTimeMs = long.MaxValue;
    private readonly ConcurrentDictionary<string, int> _rulesetMatchCount = new();
    private readonly ConcurrentDictionary<string, int> _ruleMatchCount = new();
    private readonly DateTime _collectionStartTime = DateTime.UtcNow;
    private readonly object _lockObject = new object();

    public EvaluationMetricsService(ILogger<EvaluationMetricsService> logger)
    {
        _logger = logger;
    }

    public void RecordEvaluation(EvaluationMetrics metrics)
    {
        lock (_lockObject)
        {
            _totalEvaluations++;

            if (metrics.Matched)
            {
                _successfulMatches++;
                
                if (!string.IsNullOrEmpty(metrics.MatchedRuleset))
                    _rulesetMatchCount.AddOrUpdate(metrics.MatchedRuleset, 1, (_, count) => count + 1);
                
                if (!string.IsNullOrEmpty(metrics.MatchedRule))
                    _ruleMatchCount.AddOrUpdate(metrics.MatchedRule, 1, (_, count) => count + 1);
            }
            else if (metrics.FallbackUsed)
            {
                _fallbackUsages++;
            }
            else
            {
                _noMatch++;
            }

            _totalEvaluationTimeMs += metrics.ElapsedMilliseconds;
            _maxEvaluationTimeMs = Math.Max(_maxEvaluationTimeMs, metrics.ElapsedMilliseconds);
            _minEvaluationTimeMs = Math.Min(_minEvaluationTimeMs, metrics.ElapsedMilliseconds);
        }

        _logger.LogDebug(
            "Evaluation recorded - OrderId: {OrderId}, Matched: {Matched}, FallbackUsed: {FallbackUsed}, TimeMs: {TimeMs}",
            metrics.OrderId, metrics.Matched, metrics.FallbackUsed, metrics.ElapsedMilliseconds);
    }

    public EvaluationMetricsSummary GetSummary()
    {
        lock (_lockObject)
        {
            var matchRate = _totalEvaluations > 0 ? (double)_successfulMatches / _totalEvaluations * 100 : 0;
            var fallbackRate = _totalEvaluations > 0 ? (double)_fallbackUsages / _totalEvaluations * 100 : 0;
            var averageTimeMs = _totalEvaluations > 0 ? (double)_totalEvaluationTimeMs / _totalEvaluations : 0;

            var summary = new EvaluationMetricsSummary
            {
                TotalEvaluations = _totalEvaluations,
                SuccessfulMatches = _successfulMatches,
                FallbackUsages = _fallbackUsages,
                NoMatch = _noMatch,
                MatchRate = matchRate,
                FallbackRate = fallbackRate,
                AverageEvaluationTimeMs = averageTimeMs,
                MaxEvaluationTimeMs = _maxEvaluationTimeMs,
                MinEvaluationTimeMs = _minEvaluationTimeMs == long.MaxValue ? 0 : _minEvaluationTimeMs,
                RulesetMatchCount = new Dictionary<string, int>(_rulesetMatchCount),
                RuleMatchCount = new Dictionary<string, int>(_ruleMatchCount),
                CollectionStartTime = _collectionStartTime,
                LastUpdated = DateTime.UtcNow
            };

            return summary;
        }
    }

    public void Reset()
    {
        lock (_lockObject)
        {
            _totalEvaluations = 0;
            _successfulMatches = 0;
            _fallbackUsages = 0;
            _noMatch = 0;
            _totalEvaluationTimeMs = 0;
            _maxEvaluationTimeMs = 0;
            _minEvaluationTimeMs = long.MaxValue;
            _rulesetMatchCount.Clear();
            _ruleMatchCount.Clear();
        }

        _logger.LogInformation("Evaluation metrics reset");
    }
}
