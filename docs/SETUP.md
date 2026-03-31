# Setup Guide

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Running Locally

```bash
# Clone the repository
git clone https://github.com/sushant246/ruleset-engine.git
cd ruleset-engine

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API
dotnet run --project src/RulesetEngine.Api

# Run tests
dotnet test
```

## API Endpoints

Once running, navigate to:
- **Scalar UI**: https://localhost:7xxx/scalar/v1
- **OpenAPI spec**: https://localhost:7xxx/openapi/v1.json

## Seed Data

The application automatically seeds two rulesets on startup:

### Ruleset One (Publisher 99990)
- Conditions: `PublisherNumber = 99990` AND `OrderMethod = POD`
- Rule 1: `BindTypeCode = PB` AND `IsCountry = US` AND `PrintQuantity <= 20` → Plant: **US**

### Ruleset Two (Publisher 99999)
- Conditions: `PublisherNumber = 99999` AND `OrderMethod = POD`
- Rule 1: `BindTypeCode = PB` AND `IsCountry = US` AND `PrintQuantity <= 20` → Plant: **US**
- Rule 2: `BindTypeCode = CV` AND `IsCountry = UK` AND `PrintQuantity <= 20` → Plant: **UK**
- Rule 3: `BindTypeCode = PB` AND `IsCountry = US` AND `PrintQuantity >= 20` → Plant: **KGL**
