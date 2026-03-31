using System.Net.Http.Json;
using RulesetEngine.Application.DTOs;

namespace RulesetEngine.AdminUI.Services;

public class RulesetApiClient
{
    private readonly HttpClient _http;

    public RulesetApiClient(HttpClient http)
    {
        _http = http;
    }

    // ── Rulesets ─────────────────────────────────────────────────────────────

    public Task<List<RulesetDto>?> GetRulesetsAsync()
        => _http.GetFromJsonAsync<List<RulesetDto>>("api/rulesets");

    public Task<RulesetDto?> GetRulesetAsync(int id)
        => _http.GetFromJsonAsync<RulesetDto>($"api/rulesets/{id}");

    public async Task<RulesetDto?> CreateRulesetAsync(SaveRulesetRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/rulesets", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RulesetDto>();
    }

    public async Task<RulesetDto?> UpdateRulesetAsync(int id, SaveRulesetRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/rulesets/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RulesetDto>();
    }

    public async Task DeleteRulesetAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/rulesets/{id}");
        response.EnsureSuccessStatusCode();
    }

    // ── Evaluation Logs ───────────────────────────────────────────────────────

    public Task<List<EvaluationLogDto>?> GetRecentLogsAsync(int count = 100)
        => _http.GetFromJsonAsync<List<EvaluationLogDto>>($"api/logs?count={count}");
}
