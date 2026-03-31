using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RulesetEngine.Application.Services;
using RulesetEngine.Domain.Interfaces;
using RulesetEngine.Domain.Services;
using RulesetEngine.FileWatcher;
using RulesetEngine.FileWatcher.Services;
using RulesetEngine.Infrastructure.Database;
using RulesetEngine.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// ── configuration ────────────────────────────────────────────────────────────
builder.Services.Configure<FileWatcherOptions>(
    builder.Configuration.GetSection(FileWatcherOptions.SectionName));

// ── infrastructure ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<RulesetDbContext>(options =>
    options.UseInMemoryDatabase("RulesetEngineFileWatcher"));

builder.Services.AddScoped<IRulesetRepository, RulesetRepository>();
builder.Services.AddScoped<IEvaluationLogRepository, EvaluationLogRepository>();

// ── application ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<RuleEvaluationEngine>();
builder.Services.AddScoped<IRuleEvaluationService, RuleEvaluationService>();

// ── file watcher ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IOrderFileProcessor, OrderFileProcessor>();
builder.Services.AddHostedService<ZipOrderWatcherService>();

var host = builder.Build();
host.Run();

// This marker prevents the implicit Program class from being public
// since FileWatcher is a Worker Service without a public Program
internal class _ProgramMarker { }
