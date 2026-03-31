global using System.Runtime.CompilerServices;

using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.Extensions.Logging;
using RulesetEngine.Api.Services;
using RulesetEngine.Application.Services;
using RulesetEngine.Domain.Interfaces;
using RulesetEngine.Domain.Services;
using RulesetEngine.Infrastructure.Database;
using RulesetEngine.Infrastructure.Repositories;

[assembly: InternalsVisibleTo("RulesetEngine.Tests")]

var finalArgs = args;

if (!args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase)) &&
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    finalArgs = args.Concat(new[] { "--urls", "https://127.0.0.1:7101;http://127.0.0.1:7100" }).ToArray();
}

var builder = WebApplication.CreateBuilder(finalArgs);

builder.AddServiceDefaults();

// ── infrastructure ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<RulesetDbContext>(options =>
    options.UseInMemoryDatabase("RulesetEngine"));

builder.Services.AddScoped<IRulesetRepository, RulesetRepository>();
builder.Services.AddScoped<IEvaluationLogRepository, EvaluationLogRepository>();

// ── application ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<RuleEvaluationEngine>();
builder.Services.AddScoped<IRuleEvaluationService, RuleEvaluationService>();
builder.Services.AddScoped<IRulesetManagementService, RulesetManagementService>();
builder.Services.AddScoped<RulesetSeedService>();

// ── controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ruleset Engine API",
        Version = "v1",
        Description = "API for evaluating orders against configured rulesets"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ruleset Engine API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapDefaultEndpoints();

// ── database initialization ──────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RulesetDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var seedService = scope.ServiceProvider.GetRequiredService<RulesetSeedService>();

    logger.LogInformation("🔄 Initializing database...");

    // Drop and recreate for clean state
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();

    logger.LogInformation("✅ Database created. Seeding rulesets from config...");

    // Always seed from config
    var configPath = Path.Combine(AppContext.BaseDirectory, "RulesetConfig.json");
    logger.LogInformation("📂 Looking for config at: {ConfigPath}", configPath);

    if (File.Exists(configPath))
    {
        logger.LogInformation("✅ Config file found! Seeding...");
        await seedService.SeedFromJsonAsync(dbContext, configPath);

        var count = await dbContext.Rulesets.CountAsync();
        var ruleCount = await dbContext.Rules.CountAsync();
        logger.LogInformation("✅ Seeding complete! Rulesets: {RulesetCount}, Rules: {RuleCount}", count, ruleCount);
    }
    else
    {
        logger.LogError("❌ Config file NOT found at: {ConfigPath}", configPath);
    }
}

app.Run();

// Required for integration test access
public partial class Program { }
