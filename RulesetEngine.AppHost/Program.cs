var builder = DistributedApplication.CreateBuilder(args);

// ── Core API ─────────────────────────────────────────────────────────────────
var api = builder.AddProject<Projects.RulesetEngine_Api>("api")
    .WithExternalHttpEndpoints();

// ── Admin UI ─────────────────────────────────────────────────────────────────
builder.AddProject<Projects.RulesetEngine_AdminUI>("adminui")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ApiBaseUrl", api.GetEndpoint("http"));

// ── File Watcher worker ───────────────────────────────────────────────────────
builder.AddProject<Projects.RulesetEngine_FileWatcher>("filewatcher");

builder.Build().Run();
