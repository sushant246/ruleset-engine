# RulesetEngine

A production-grade, rule-based order evaluation system built with .NET 10 and Clean Architecture principles. Evaluates orders against configurable rulesets to determine production plants, with support for multiple deployment scenarios (REST API, Blazor UI, background worker service).

**Status:** ✅ Production Ready | **Version:** 1.0.0 | **License:** MIT

---

## 📚 Table of Contents
1. [Quick Start](#quick-start)
2. [Features](#features)
3. [Project Structure](#project-structure)
4. [Database](#-database)
5. [API Endpoints](#api-endpoints)
6. [FileWatcher Worker Service](#-filewatcher-worker-service)
7. [Aspire Integration](#aspire-integration)
8. [Configuration](#configuration)
9. [Testing](#testing)
10. [Database Migrations](#-database-migrations)
11. [Documentation](#documentation)
12. [Project Statistics](#-project-statistics)
13. [Troubleshooting](#troubleshooting)
14. [Contributing](#contributing)
15. [Support](#support)

---

## 🚀 Quick Start

### Prerequisites
- .NET 10 SDK
- Visual Studio 2022/2026 or VS Code
- PowerShell 7+

### Setup & Run

#### 1. Clone Repository
```bash
git clone https://github.com/sushant246/ruleset-engine.git
cd ruleset-engine
```

#### 2. Restore & Build
```bash
dotnet restore
dotnet build
```

#### 3. Run with Aspire Orchestration (Recommended)
```bash
cd src/RulesetEngine.AppHost
dotnet run
```
- **Aspire Dashboard:** http://localhost:17890
- **API Swagger:** http://localhost:7101/swagger
- **AdminUI Blazor:** http://localhost:7001

> **Note:** Aspire orchestrates all services automatically. Open the dashboard to view logs, metrics, and service health.

#### 4. Run Individual Services

**API Service Only:**
```bash
cd src/RulesetEngine.Api
dotnet run
```
Swagger available at: https://localhost:7101/swagger

**FileWatcher Worker:**
```bash
cd src/RulesetEngine.FileWatcher
dotnet run
```
Monitors `orders/incoming` folder for ZIP files containing order JSON files.

**Admin UI (Blazor):**
```bash
cd src/RulesetEngine.AdminUI
dotnet run
```

### Run Tests
```bash
dotnet test tests/RulesetEngine.Tests --verbosity normal
```

## 📋 Features

✅ **Rule-Based Evaluation** - Flexible condition-based rule engine with AND logic
✅ **Multiple Deployment Options** - REST API, Blazor UI, Background Worker
✅ **In-Memory Database** - Fast, lightweight, development-friendly (EF Core)
✅ **Caching Layer** - 60-minute TTL ruleset caching with automatic invalidation
✅ **Fallback Support** - Configurable fallback plant if no rules match
✅ **Aspire Integration** - Cloud-native ready with OpenTelemetry & health checks
✅ **File Processing** - ZIP-based batch order processing for FileWatcher
✅ **Comprehensive Logging** - Structured logging with correlation IDs
✅ **Full Test Coverage** - Unit, extended, and integration tests

## 🏗️ Project Structure

```
RulesetEngine/
├── src/
│   ├── RulesetEngine.Domain/              # Layer 1: Business Logic (Core)
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── Services/
│   │
│   ├── RulesetEngine.Infrastructure/      # Layer 2: Data Access
│   │   ├── Database/
│   │   └── Repositories/
│   │
│   ├── RulesetEngine.Application/         # Layer 3: Use Cases
│   │   ├── Services/
│   │   └── DTOs/
│   │
│   ├── RulesetEngine.Api/                 # Layer 4: REST API
│   ├── RulesetEngine.AdminUI/             # Layer 4: Blazor UI
│   ├── RulesetEngine.FileWatcher/         # Layer 4: Worker Service
│   │
│   ├── RulesetEngine.AppHost/             # Aspire Orchestration
│   └── RulesetEngine.ServiceDefaults/     # Shared Infrastructure
│
└── tests/
    └── RulesetEngine.Tests/               # Unit & Integration Tests
```

## 💾 Database

### EF Core In-Memory Database
RulesetEngine uses **Entity Framework Core's In-Memory database** for development and testing.

**Why In-Memory?**
- ✅ Zero setup - no SQL Server needed
- ✅ Fast development cycles
- ✅ Perfect for testing & demos
- ✅ Thread-safe & ACID compliant

**For Production:** Update to SQL Server, PostgreSQL, or SQLite in Program.cs

## 🔧 API Endpoints

### Evaluate Order
```http
POST /api/evaluate
```

### Get Evaluation Logs
```http
GET /api/logs?count=50
```

## 📤 FileWatcher Worker Service

Monitors `orders/incoming` for ZIP files with order JSON files and processes them in batches.

**Configuration:** Edit `appsettings.json`:
```json
{
  "FileWatcher": {
    "WatchFolder": "orders/incoming",
    "ArchiveFolder": "orders/archive",
    "ErrorFolder": "orders/error"
  }
}
```

## ☁️ Aspire Integration

Run all services together:
```bash
cd src/RulesetEngine.AppHost
dotnet run
```

**Dashboard:** http://localhost:17890

## ⚙️ Configuration

### Fallback Production Plant
```json
{
  "RulesetEngine": {
    "FallbackProductionPlant": "DefaultPlant"
  }
}
```

### Cache Configuration
```json
{
  "Caching": {
    "RulesetCacheTtlMinutes": 60,
    "EnableCaching": true
  }
}
```

### Database Connection String
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=RulesetEngine;Trusted_Connection=true;"
  }
}
```

---

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "ClassName=RuleEvaluationEngineTests"
```

### Run with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

### Run with Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

**Test Coverage:**
- ✅ Unit Tests (Domain Layer)
- ✅ Unit Tests (Application Layer)
- ✅ Integration Tests (End-to-end)
- ✅ Edge Cases & Error Handling
- ✅ Coverage > 80%

---

## 🔄 Database Migrations

### Switching to SQL Server

**Step 1: Generate Migration**
```bash
dotnet ef migrations add InitialCreate `
  --project src/RulesetEngine.Infrastructure `
  --startup-project src/RulesetEngine.Api
```

**Step 2: Update Database**
```bash
dotnet ef database update `
  --project src/RulesetEngine.Infrastructure `
  --startup-project src/RulesetEngine.Api
```

**Step 3: Update Program.cs**
```csharp
// Change from In-Memory:
builder.Services.AddDbContext<RulesetDbContext>(options =>
    options.UseInMemoryDatabase("RulesetEngineDb"));

// To SQL Server:
builder.Services.AddDbContext<RulesetDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### PostgreSQL Support
```bash
# In Program.cs, change to:
builder.Services.AddDbContext<RulesetDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

# Then apply migration
dotnet ef database update
```

See **DesignDocument.md** for complete SQL scripts.

---

## 📖 Documentation

### Complete Technical Documentation
- **DesignDocument.md** (1200+ lines)
  - System overview & business rules
  - 28 documented assumptions
  - Design philosophy & reasoning
  - Clean Architecture (5 layers)
  - 4 comprehensive architecture diagrams
  - 7 design patterns with code examples
  - 5 technical decisions with rationale
  - Database schema & ER diagram
  - SQL migration scripts (SQL Server & PostgreSQL)
  - EF Core migration code
  - API endpoint specifications
  - Complete testing strategy
  - Deployment architecture

- **DOCUMENTATION_CHECKLIST.md** - Verification of all requirements

---

## 📊 Project Statistics

| Metric | Value |
|--------|-------|
| **Framework** | .NET 10 |
| **Architecture** | Clean Architecture (5 layers) |
| **Database** | EF Core (In-Memory, SQL Server, PostgreSQL) |
| **UI** | Blazor Server-side |
| **API** | ASP.NET Core 10 |
| **Testing** | MSTest |
| **Documentation** | 1500+ lines |
| **Code Examples** | 25+ |
| **Design Patterns** | 7 |
| **Test Scenarios** | 15+ |

---

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| **Database is locked** | Ensure only one instance running or clear in-memory DB |
| **Port already in use (7101)** | Update `launchSettings.json` or stop other processes |
| **FileWatcher not processing** | Ensure `orders/incoming` exists and has permissions |
| **Aspire Dashboard unavailable** | Check port 17890 availability or firewall settings |

---

## 🤝 Contributing

### Guidelines
1. Fork repository
2. Create feature branch: `git checkout -b feature/your-feature`
3. Commit changes: `git commit -am 'Add your feature'`
4. Push to branch: `git push origin feature/your-feature`
5. Submit Pull Request

### Before Submitting PR
- ✅ `dotnet build` succeeds
- ✅ All tests pass: `dotnet test`
- ✅ Documentation updated
- ✅ No breaking changes

### Code Standards
- Follow Microsoft C# naming conventions
- Use async/await for I/O
- Add XML documentation for public APIs
- Write tests for new features

---

## 📞 Support

- **Documentation:** See DesignDocument.md
- **API Reference:** View Swagger at `/swagger` when running
- **Issues:** Create GitHub issue with details
- **Questions:** Check documentation and test examples

---

## 📋 Quick Commands

```bash
# Build the project
dotnet build

# Run all tests
dotnet test

# Run API only
cd src/RulesetEngine.Api && dotnet run

# Run with Aspire (recommended)
cd src/RulesetEngine.AppHost && dotnet run

# Run Admin UI only
cd src/RulesetEngine.AdminUI && dotnet run

# Run FileWatcher only
cd src/RulesetEngine.FileWatcher && dotnet run

# Clean and rebuild
dotnet clean && dotnet build
```

---

## 📄 License

MIT License - see LICENSE file for details.

---

## 🎯 Next Steps

1. **Read Quick Start** above to get running
2. **Review DesignDocument.md** for architecture details
3. **Explore `/api/swagger`** to test endpoints
4. **Check test examples** in `tests/RulesetEngine.Tests`
5. **Review DOCUMENTATION_CHECKLIST.md** for coverage verification

---

**Built with:** .NET 10 | Clean Architecture | Aspire | EF Core | Blazor  
**Status:** ✅ Production Ready | **Version:** 1.0.0
