# Architecture Overview

## Clean Architecture Layers

```
┌─────────────────────────────────────────────────────┐
│                   API Layer                          │
│           (RulesetEngine.Api)                        │
│   Controllers, Middleware, Program.cs                │
├─────────────────────────────────────────────────────┤
│               Application Layer                      │
│         (RulesetEngine.Application)                  │
│      DTOs, Services, Use Cases                       │
├─────────────────────────────────────────────────────┤
│              Infrastructure Layer                    │
│         (RulesetEngine.Infrastructure)               │
│   EF Core DbContext, Repositories, Database          │
├─────────────────────────────────────────────────────┤
│                 Domain Layer                         │
│           (RulesetEngine.Domain)                     │
│   Entities, Interfaces, Domain Services              │
└─────────────────────────────────────────────────────┘
```

## Project Structure

```
ruleset-engine/
├── src/
│   ├── RulesetEngine.Domain/          # Core business logic
│   │   ├── Entities/                  # Domain entities
│   │   ├── Interfaces/                # Repository contracts
│   │   └── Services/                  # Domain services
│   ├── RulesetEngine.Infrastructure/  # Data access
│   │   ├── Database/                  # EF Core DbContext
│   │   └── Repositories/              # Repository implementations
│   ├── RulesetEngine.Application/     # Use cases
│   │   ├── DTOs/                      # Data transfer objects
│   │   └── Services/                  # Application services
│   └── RulesetEngine.Api/             # HTTP API
│       ├── Controllers/               # MVC controllers
│       └── Middleware/                # Custom middleware
├── tests/
│   └── RulesetEngine.Tests/           # Unit and integration tests
│       ├── Domain/
│       ├── Application/
│       └── Api/
└── docs/                              # Documentation
```

## Domain Model

### Ruleset
A **Ruleset** groups related rules for a specific publisher or context. It has:
- **Conditions**: Top-level conditions that must match before evaluating rules.
- **Rules**: Ordered list of rules to evaluate when ruleset conditions match.
- **Priority**: Determines which ruleset is evaluated first.

### Rule
A **Rule** within a ruleset defines specific conditions and a result:
- **Conditions**: Field-based conditions (e.g., `BindTypeCode Equals PB`)
- **Result**: The `ProductionPlant` to assign when all conditions match.

### Evaluation Flow
1. Load all active rulesets ordered by priority.
2. For each ruleset, evaluate its top-level conditions.
3. If ruleset conditions match, evaluate each rule in priority order.
4. Return the first matching rule's production plant.
5. If no match found, return unmatched result.

## Supported Operators
| Operator | Description |
|---|---|
| `Equals` | Exact match (case-insensitive) |
| `NotEquals` | Not equal (case-insensitive) |
| `Contains` | Field contains value |
| `StartsWith` | Field starts with value |
| `EndsWith` | Field ends with value |
| `GreaterThan` | Numeric greater than |
| `GreaterThanOrEqual` | Numeric greater than or equal |
| `LessThan` | Numeric less than |
| `LessThanOrEqual` | Numeric less than or equal |
