var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.RulesetEngine_Api>("ruleset-engine-eval");

builder.Build().Run();
