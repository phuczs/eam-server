# EAM Platform — Educational Account Management System

A .NET 10 Clean Architecture starter for an Educational Account Management System.
The domain is intentionally empty (**no entities, no API controllers, and no
domain-service implementations yet** — just representative interface/DTO/options
stubs), but the **cross-cutting infrastructure is fully implemented**: middleware
pipeline, multi-tier rate limiting, email (templates + Gmail/log senders), Azure
blob storage, caching (memory + Redis), PBKDF2 password hashing, auditing, and
CSV/Excel file I/O — all wired through dependency injection.

## Solution layout

```
EAM.Platform.sln
├── src/
│   ├── EAM.Domain/            # entities, enums, abstractions  (EMPTY — add domain here)
│   ├── EAM.Application/       # use-case layer
│   │   ├── Common/            # ApiResponse, ErrorResponse, AppException, CurrentUser, paging, ExportResult
│   │   ├── DTOs/              # Account/ + Auth/ request & response placeholders
│   │   ├── Email/             # EmailTemplates (branded HTML)
│   │   ├── Interfaces/
│   │   │   ├── Services/      # IAccount/IUser/IAuth/IRole/IEmail/IFile service contracts
│   │   │   ├── Repositories/  # (empty)
│   │   │   └── Infrastructures/ # ICurrentUserAccessor, IEmailSender, IEmailWhitelist, IBlobStorage, IPasswordHasher, ICache, IAuditWriter ...
│   │   ├── Mappings/          # AutoMapper MappingProfile
│   │   ├── Options/           # AzureAd, Singpass/Mockpass, BlobStorage, Gmail
│   │   ├── Services/EmailService.cs  # template wrapper over IEmailSender
│   │   └── DependencyInjection.cs    # AddApplication()
│   ├── EAM.Infrastructure/    # adapters
│   │   ├── Caching/           # MemoryLookupCache, RedisCacheService (+ distributed lock)
│   │   ├── Email/             # GmailEmailSender, WhitelistedEmailSender (default), Hangfire dispatcher, AllowAllEmailWhitelist
│   │   ├── Helper/FileService.cs     # CSV/Excel read & write
│   │   ├── Logging/AuditWriter.cs    # structured audit log (swap for DB when entities land)
│   │   ├── Security/Pbkdf2PasswordHasher.cs
│   │   ├── Storage/AzureBlobStorageService.cs
│   │   ├── Migrations/        # EF Core migrations (EMPTY)
│   │   ├── Persistence/DbSeeder.cs   # empty seeder
│   │   └── DependencyInjection.cs    # AddInfrastructure(config)
│   └── EAM.Api/              # ASP.NET Core host
│       ├── Configuration/    # ApiResponseWrapperFilter, HttpCurrentUserAccessor, HttpRequestContext
│       ├── Custom/SecureJsonResult.cs
│       ├── Middleware/       # DebugContext, RequestLogging, PerformanceLogging, ExceptionHandling, Security
│       ├── RateLimiting/CircuitBreakerRateLimiter.cs
│       ├── Infrastructure/   # (empty — host filters/adapters go here)
│       ├── Extensions/       # ServiceCollectionExtensions, MiddlewareExtensions, AddCustomRateLimiter
│       ├── Properties/launchSettings.json
│       ├── appsettings.json            # config shape incl. AzureAd + Singpass/Mockpass placeholders
│       ├── appsettings.Development.json
│       └── Program.cs
├── tests/EAM.Tests/          # xUnit Unit/ + Integration/ (WebApplicationFactory)
├── EAM.E2E.Tests/            # Playwright (NUnit) end-to-end skeleton
├── scripts/ci/*.ps1          # build / test / package / deploy / rollback / smoke
├── db/init.sql               # placeholder bootstrap SQL
└── .gitlab-ci.yml            # build → test → package → deploy → smoke
```

## Prerequisites

- .NET SDK 10
- (Optional, later) SQL Server + Redis once persistence/caching are wired up

## Build, run, test

```powershell
dotnet restore EAM.Platform.sln
dotnet build   EAM.Platform.sln -c Release

# Run the API (Swagger at /swagger in Development, health at /health)
dotnet run --project src/EAM.Api/EAM.Api.csproj

# Tests (the CI scripts wrap these and emit JUnit reports)
pwsh -File scripts/ci/unit-tests.ps1
pwsh -File scripts/ci/integration-tests.ps1
```

## External identity placeholders

`appsettings.json` ships two **placeholder** sections so the integration shape is
documented before the real wiring lands:

- **`AzureAd`** — Microsoft Entra ID (AAD). Bound to `EAM.Application.Options.AzureAdOptions`.
- **`Singpass`** — Singpass/Corppass. In development it points at
  [Mockpass](https://github.com/opengovsg/mockpass), a local emulator. Bound to
  `EAM.Application.Options.SingpassOptions` (with a nested `Mockpass` block).

Both are registered (`services.Configure<...>`) in `EAM.Infrastructure/DependencyInjection.cs`
but not yet consumed — add the authentication handlers when those flows are built.

## Filling in the skeleton

1. Add entities/enums under `src/EAM.Domain`.
2. Add an EF Core `DbContext` + configurations + repositories in `EAM.Infrastructure`,
   then `Add-Migration` into `EAM.Infrastructure/Migrations` and flesh out `DbSeeder`.
3. Implement the services behind the interfaces in `EAM.Application/Interfaces/Services`
   and register them in `AddApplication()` / `AddInfrastructure()`.
4. Add controllers under `src/EAM.Api/Controllers` (folder intentionally omitted for now).
