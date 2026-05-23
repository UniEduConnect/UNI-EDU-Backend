# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Commands

All `dotnet` commands are run from the repository root unless noted otherwise.

```powershell
# Restore + build the whole solution
dotnet build UNI-EDU-Backend.slnx

# Run the API (default profile binds the dev URLs from launchSettings.json)
dotnet run --project UNI-EDU-Backend.API

# Start PostgreSQL (PostGIS 15-3.4) — required for the API and EF migrations
docker compose up -d postgres

# EF Core migrations (Infrastructure holds the DbContext, API is the startup project)
dotnet ef migrations add <Name> --project UNI-EDU-Backend.Infrastructure --startup-project UNI-EDU-Backend.API
dotnet ef database update     --project UNI-EDU-Backend.Infrastructure --startup-project UNI-EDU-Backend.API
```

Swagger UI is served at `/swagger` when `ASPNETCORE_ENVIRONMENT=Development`. CORS is hard-coded to allow `http://localhost:3000` (the frontend) — change `Program.cs` if a different origin is needed.

There is no test project in the solution today.

## Configuration

Database credentials are read from a `.env` file at the repo root (loaded via `DotNetEnv` in both `Program.cs` and `ApplicationDbContextFactory.cs` with the path `"../.env"` — i.e. relative to each project's working directory). Required keys: `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_HOST`, `POSTGRES_PORT`, `PGTZ`. `appsettings.json` does **not** carry the connection string; do not move config there without also updating the loader.

## Architecture

Clean / onion-layered solution targeting **.NET 10**. Dependencies flow inward: API → Application → Domain, and Infrastructure → Application → Domain. Domain has no project references.

- **UNI-EDU-Backend.Domain** — POCO entities (`Models/`) and enums. Entity relationships (1-1 User↔Tutor/Parent/Student, ExamQuestion composite key, cascade rules) are configured in `ApplicationDbContext.OnModelCreating`, not via data annotations.
- **UNI-EDU-Backend.Application** — Use-case layer. Service interfaces under `Services/<Feature>/I<Feature>Service.cs` with implementations next to them. Repository **interfaces** live here (`Interfaces/`), implementations live in Infrastructure — keep this direction. DTOs are grouped by feature folder under `DTOs/`; each request DTO has a sibling `*Validator.cs` (FluentValidation). Custom exceptions in `Exceptions/` all derive from `ApplicationException` and carry a `Title` consumed by the global handler.
- **UNI-EDU-Backend.Infrastructure** — `ApplicationDbContext`, `Migrations/`, and `Repositories/`. `ApplicationDbContextFactory` is a design-time factory so `dotnet ef` works without booting the API.
- **UNI-EDU-Backend.API** — Controllers, middleware, and composition root (`Program.cs`). Services and repositories are wired manually with `AddScoped`; **MediatR** is registered across the Application + Infrastructure assemblies with a `ValidationBehavior<,>` pipeline (note: handlers are MediatR-style but the current `UsersController` still calls `IUserService` directly — both patterns coexist).

### Cross-cutting conventions

- **Responses**: Controllers wrap payloads in `ApiResponse<T>` (`UNI-EDU-Backend.API/Commons/ApiResponse.cs`) with `StatusCode`, `Message`, `Data`.
- **Errors**: Throw the typed exceptions from `Application/Exceptions/` (`NotFoundException`, `BadRequestException`, `ValidationException`, `UnauthorizedAccessException`, `ForbiddenAccessException`). `GlobalExceptionHandlerMiddleware` maps them to HTTP status codes and serializes an `ErrorResponse` with camelCase JSON. Do not catch these in controllers.
- **Validation**: FluentValidation runs automatically through `ValidationBehavior<TRequest, TResponse>` for any `IRequest<>` that has registered validators — validators are picked up from the Application assembly via `AddValidatorsFromAssembly`. Failures throw `ValidationException` with a per-field error dictionary that the middleware surfaces as `errors[]`.
- **Mapping**: Use **Mapster** (`source.Adapt<TDest>()`) for entity↔DTO conversion (see `UserService.CheckPhoneNumberAsync`).
- **Adding a feature**: Domain entity → DbSet on `ApplicationDbContext` (+ relationship config in `OnModelCreating` if needed) → migration → repository interface in Application + implementation in Infrastructure → service interface + implementation in Application → DTOs + validator → controller endpoint → register the service/repository in `Program.cs`.

## Branching & PRs

Default base for PRs is `dev` (not `main`). CodeRabbit auto-reviews PRs targeting `main` or `dev` (see `.coderabbit.yaml`).
