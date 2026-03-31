global using System.Runtime.CompilerServices;

using RulesetEngine.Api;

[assembly: InternalsVisibleTo("RulesetEngine.Tests")]

var finalArgs = args;

if (!args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase)) &&
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    finalArgs = args.Concat(new[] { "--urls", "https://127.0.0.1:7101;http://127.0.0.1:7100" }).ToArray();
}

var app = ProgramSetup.CreateApp(finalArgs);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RulesetEngine.Infrastructure.Database.RulesetDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await ProgramSetup.SeedDatabase(dbContext);
}

app.Run();

// Required for integration test access
public partial class Program { }
