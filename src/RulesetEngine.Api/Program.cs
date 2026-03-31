using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using RulesetEngine.Api.Middleware;
using RulesetEngine.Application.Services;
using RulesetEngine.Domain.Interfaces;
using RulesetEngine.Domain.Services;
using RulesetEngine.Infrastructure.Database;
using RulesetEngine.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<RulesetDbContext>(options =>
    options.UseInMemoryDatabase("RulesetEngine"));

builder.Services.AddScoped<IRulesetRepository, RulesetRepository>();
builder.Services.AddScoped<IEvaluationLogRepository, EvaluationLogRepository>();

builder.Services.AddScoped<RuleEvaluationEngine>();
builder.Services.AddScoped<IRuleEvaluationService, RuleEvaluationService>();
builder.Services.AddScoped<IRulesetManagementService, RulesetManagementService>();

builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<RulesetDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await SeedDatabase(dbContext);
}

app.Run();

static async Task SeedDatabase(RulesetDbContext dbContext)
{
    if (dbContext.Rulesets.Any())
        return;

    var rulesetOne = new RulesetEngine.Domain.Entities.Ruleset
    {
        Name = "Ruleset One",
        Description = "Ruleset for Publisher 99990",
        Priority = 1,
        IsActive = true,
        Conditions = new List<RulesetEngine.Domain.Entities.Condition>
        {
            new() { Field = "PublisherNumber", Operator = "Equals", Value = "99990" },
            new() { Field = "OrderMethod", Operator = "Equals", Value = "POD" }
        },
        Rules = new List<RulesetEngine.Domain.Entities.Rule>
        {
            new()
            {
                Name = "Rule 1",
                Priority = 1,
                Conditions = new List<RulesetEngine.Domain.Entities.Condition>
                {
                    new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" },
                    new() { Field = "IsCountry", Operator = "Equals", Value = "US" },
                    new() { Field = "PrintQuantity", Operator = "LessThanOrEqual", Value = "20" }
                },
                Result = new RulesetEngine.Domain.Entities.RuleResult { ProductionPlant = "US" }
            }
        }
    };

    var rulesetTwo = new RulesetEngine.Domain.Entities.Ruleset
    {
        Name = "Ruleset Two",
        Description = "Ruleset for Publisher 99999",
        Priority = 2,
        IsActive = true,
        Conditions = new List<RulesetEngine.Domain.Entities.Condition>
        {
            new() { Field = "PublisherNumber", Operator = "Equals", Value = "99999" },
            new() { Field = "OrderMethod", Operator = "Equals", Value = "POD" }
        },
        Rules = new List<RulesetEngine.Domain.Entities.Rule>
        {
            new()
            {
                Name = "Rule 1",
                Priority = 1,
                Conditions = new List<RulesetEngine.Domain.Entities.Condition>
                {
                    new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" },
                    new() { Field = "IsCountry", Operator = "Equals", Value = "US" },
                    new() { Field = "PrintQuantity", Operator = "LessThanOrEqual", Value = "20" }
                },
                Result = new RulesetEngine.Domain.Entities.RuleResult { ProductionPlant = "US" }
            },
            new()
            {
                Name = "Rule 2",
                Priority = 2,
                Conditions = new List<RulesetEngine.Domain.Entities.Condition>
                {
                    new() { Field = "BindTypeCode", Operator = "Equals", Value = "CV" },
                    new() { Field = "IsCountry", Operator = "Equals", Value = "UK" },
                    new() { Field = "PrintQuantity", Operator = "LessThanOrEqual", Value = "20" }
                },
                Result = new RulesetEngine.Domain.Entities.RuleResult { ProductionPlant = "UK" }
            },
            new()
            {
                Name = "Rule 3",
                Priority = 3,
                Conditions = new List<RulesetEngine.Domain.Entities.Condition>
                {
                    new() { Field = "BindTypeCode", Operator = "Equals", Value = "PB" },
                    new() { Field = "IsCountry", Operator = "Equals", Value = "US" },
                    new() { Field = "PrintQuantity", Operator = "GreaterThanOrEqual", Value = "20" }
                },
                Result = new RulesetEngine.Domain.Entities.RuleResult { ProductionPlant = "KGL" }
            }
        }
    };

    dbContext.Rulesets.Add(rulesetOne);
    dbContext.Rulesets.Add(rulesetTwo);
    await dbContext.SaveChangesAsync();
}

// Required for integration test access
public partial class Program { }
