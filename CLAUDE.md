# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Common Commands

All `dotnet` commands are run from the repository root unless noted otherwise.

```powershell
# Restore + build the whole solution
dotnet build UNI-EDU-Backend.slnx

# Run the API (http profile binds http://localhost:5115; https profile adds https://localhost:7271)
dotnet run --project UNI-EDU-Backend.API

# Start PostgreSQL (PostGIS 15-3.4) — required for the API and EF migrations
docker compose up -d postgres

# EF Core migrations (Infrastructure holds the DbContext, API is the startup project)
dotnet ef migrations add <Name> --project UNI-EDU-Backend.Infrastructure --startup-project UNI-EDU-Backend.API
dotnet ef database update     --project UNI-EDU-Backend.Infrastructure --startup-project UNI-EDU-Backend.API
```

Swagger UI is served at `/swagger` when `ASPNETCORE_ENVIRONMENT=Development` (it's also the default `launchUrl`). CORS allows `http://localhost:3000` and `http://localhost:8080` — change `Program.cs` if a different origin is needed.

There is no test project in the solution today.

## Configuration

Database credentials are read from a `.env` file at the repo root, loaded via `DotNetEnv` in both [Program.cs](UNI-EDU-Backend.API/Program.cs) and [ApplicationDbContextFactory.cs](UNI-EDU-Backend.Infrastructure/ApplicationDbContextFactory.cs) with the path `"../.env"` — i.e. relative to each project's working directory. Required keys: `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_HOST`, `POSTGRES_PORT`, `PGTZ`. `appsettings.json` only carries the JWT secret (`Jwt:SecretKey`); the connection string is **not** there — do not move config without also updating the loader.

`Jwt:SecretKey` falls back to the `JWT_SecretKey` env var if not set in `appsettings.json`. Production deployments should override it.

## Architecture

Clean / onion-layered solution targeting **.NET 10**. Dependencies flow inward: API → Application → Domain, and Infrastructure → Application → Domain. Domain has no project references.

- **UNI-EDU-Backend.Domain** — POCO entities ([Models/](UNI-EDU-Backend.Domain/Models/)) and enums ([Enums/](UNI-EDU-Backend.Domain/Enums/)). Entity relationships (1-1 User↔Tutor/Parent/Student, M2M Tutor↔Subject, ExamQuestion composite key, cascade rules, Postgres `jsonb`/`text[]` column types) are configured in [`ApplicationDbContext.OnModelCreating`](UNI-EDU-Backend.Infrastructure/ApplicationDbContext.cs), **not** via data annotations.
- **UNI-EDU-Backend.Application** — Use-case layer. Service interfaces under `Services/<Feature>/I<Feature>Service.cs` with implementations next to them. Repository **interfaces** live here under [Interfaces/](UNI-EDU-Backend.Application/Interfaces/) (namespace: `UNI_EDU_Backend.Application.Interfaces.Repositories`); implementations live in Infrastructure — keep this direction. DTOs are grouped by feature folder under [DTOs/](UNI-EDU-Backend.Application/DTOs/); each request DTO has a sibling `*Validator.cs` (FluentValidation). Custom exceptions in [Exceptions/](UNI-EDU-Backend.Application/Exceptions/) all derive from `ApplicationException` and carry a `Title` consumed by the global handler.
- **UNI-EDU-Backend.Infrastructure** — [`ApplicationDbContext`](UNI-EDU-Backend.Infrastructure/ApplicationDbContext.cs), [Migrations/](UNI-EDU-Backend.Infrastructure/Migrations/), and [Repositories/](UNI-EDU-Backend.Infrastructure/Repositories/). [`ApplicationDbContextFactory`](UNI-EDU-Backend.Infrastructure/ApplicationDbContextFactory.cs) is a design-time factory so `dotnet ef` works without booting the API.
- **UNI-EDU-Backend.API** — Controllers, middleware, and composition root ([Program.cs](UNI-EDU-Backend.API/Program.cs)). Services and repositories are wired manually with `AddScoped`. **There is no MediatR** — validators are registered via `AddValidatorsFromAssembly(...)` and invoked manually from services via the [`EnsureValidAsync`](UNI-EDU-Backend.Application/Commons/ValidatorExtensions.cs) extension.

### Cross-cutting conventions

- **Responses**: Controllers wrap payloads in `ApiResponse<T>` ([UNI-EDU-Backend.API/Commons/ApiResponse.cs](UNI-EDU-Backend.API/Commons/ApiResponse.cs)) with `StatusCode`, `Message`, `Data`. For paged collections, the `Data` is a `PagedResult<T>` from [UNI-EDU-Backend.Application/Commons/PagedResult.cs](UNI-EDU-Backend.Application/Commons/PagedResult.cs) (`Items`, `Total`, `Page`, `PageSize`, computed `TotalPages`).
- **Errors**: Throw the typed exceptions from [Application/Exceptions/](UNI-EDU-Backend.Application/Exceptions/) (`NotFoundException`, `BadRequestException`, `ValidationException`, `UnauthorizedAccessException`, `ForbiddenAccessException`). [`GlobalExceptionHandlerMiddleware`](UNI-EDU-Backend.API/Middleware/GlobalExceptionHandlerMiddleware.cs) maps them to HTTP status codes and serializes an `ErrorResponse` (camelCase). Do not catch these in controllers.
- **Validation**: Validators live next to their DTOs (`*Validator.cs`) and inherit `AbstractValidator<T>`. Call `_validator.EnsureValidAsync(request, ct)` from the service before doing work — this throws `ValidationException` with a per-field error dictionary that the middleware surfaces as `errors[]`. There is **no automatic pipeline** wrapper around handlers, so forgetting the call means no validation runs.
- **Mapping**: **AutoMapper** is the registered mapper (`AddAutoMapper(typeof(MappingProfile))` in `Program.cs`) and is the one used in production paths (see [`AuthService`](UNI-EDU-Backend.Application/Services/Auths/AuthService.cs) using `IMapper`). Mapster is referenced in the `.csproj` but currently unused — pick AutoMapper for consistency unless you have a strong reason. Profiles live in [Application/Mappings/](UNI-EDU-Backend.Application/Mappings/).
- **Auth**: JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`). Access tokens are 1h; refresh tokens are random 32-byte base64 stored in the `RefreshTokens` table and set on the client as an `HttpOnly; Secure; SameSite=Strict` cookie named `refreshToken`. The refresh endpoint reads the cookie, rotates the token (marks old `IsUsed` + `IsRevoked`), and issues a new pair. Validation skips issuer/audience and accepts a 30s clock skew. JWT events log auth failures to stderr — silence those in production.
- **Claims**: Controllers that need the caller's identity read `ClaimTypes.NameIdentifier` (user id, GUID) and `ClaimTypes.Role` (`Admin` / `Tutor` / `Student` / `Parent`). See [`ClassesController.ReadCallerOrThrow`](UNI-EDU-Backend.API/Controllers/ClassesController.cs) for the canonical pattern — throw `UnauthorizedAccessException` if either claim is missing/invalid.
- **Time on the wire**: [`FlexibleTimeOnlyConverter`](UNI-EDU-Backend.API/Json/FlexibleTimeOnlyConverter.cs) accepts `HH:mm` and `HH:mm:ss[.fff]` (with optional trailing `Z`) for `TimeOnly` properties. Registered globally so frontend schedule pickers that emit `"19:00"` Just Work. Output is always `HH:mm:ss`.
- **Generic repo**: [`IGenericRepository<T>`](UNI-EDU-Backend.Application/Interfaces/IGenericRepository.cs) + [`IUnitOfWork.SaveChangesAsync()`](UNI-EDU-Backend.Application/Interfaces/IUnitOfWork.cs) are available for simple CRUD. Most features use a **feature-specific repository** instead (e.g. `IClassRepository`, `ITutorRepository`) so the query shapes — `.Select` projections, `EF.Functions.ILike`, transactions — stay close to the data. Reach for the generic repo only for trivial entity reads/writes (refresh tokens).
- **Adding a feature**: Domain entity → DbSet on `ApplicationDbContext` (+ relationship config in `OnModelCreating` if needed) → migration → repository interface in Application + implementation in Infrastructure → service interface + implementation in Application (call `_validator.EnsureValidAsync` first) → DTOs + validator → controller endpoint (wrap in `ApiResponse<T>`) → register the service/repository in `Program.cs`.

### Domain model highlights

- **Users & roles**: `User` is the identity row; `Tutor`/`Student`/`Parent` are 1-1 specializations keyed by the same GUID as the user (`TutorID == UserID`). Role is stored as the `UserRole` enum on `User`.
- **Tutor**: rich profile (avatar, location, school, hourly rate, years experience, verification, teaching style, intro video, `TutorType` enum) plus three Postgres-typed collections — `Certificates text[]`, `Achievements text[]`, `AvailableSlots jsonb` (list of `{ Day, Time }`). M2M to `Subject` via the explicit `TutorSubject` join entity (composite key `{TutorID, SubjectID}`). Configured in `OnModelCreating`.
- **Class + escrow**: when a class is created the API debits `Wallet.Balance` and credits `Wallet.EscrowBalance` for the full `Fee`, writes a `WalletTransaction { Type = EscrowIn }`, and pre-generates placeholder `Session` rows by walking the calendar forward from `StartDate` against `WeeklySlots` (stored as `jsonb`). All four writes happen in one EF transaction — see [`ClassRepository.CreateClassWithEscrowAsync`](UNI-EDU-Backend.Infrastructure/Repositories/ClassRepository.cs). Status starts as `ClassStatus.Searching`, escrow at `EscrowStatus.Pending`. `ClassMaterial` and `Session` are child entities that cascade-delete with the class.
- **Authorization for class access**: `ClassService` enforces caller-vs-class rules in code — `Tutor`/`Student` must match their own ID on the class; `Parent` must be the parent of the booking `Student` (via `IsParentOfStudentAsync`); `Admin` sees everything. `ParentName` is the only parent-derived field surfaced on `ClassDetailResponse`; do not leak `ParentID`.

## Endpoints

All routes live under `/api`. Controllers use the `[Route("api/[controller]")]` convention except [AuthController](UNI-EDU-Backend.API/Controllers/AuthController.cs), which explicitly routes to `/api/login`, `/api/register/{role}`, `/api/refresh-token`, `/api/logout`.

| Method | Route | Auth | Notes |
| --- | --- | --- | --- |
| POST | `/api/login` | public | Body: `LoginRequest`. Returns `TokenResponse` + sets `refreshToken` cookie. |
| POST | `/api/register/student` | public | Body: `StudentRegister`. |
| POST | `/api/register/tutor` | public | Body: `TutorRegister`. |
| POST | `/api/refresh-token` | cookie | Rotates the token from the `refreshToken` cookie. |
| POST | `/api/logout` | public | Clears the cookie. |
| POST | `/api/users/check-phone` | public | OTP flow precheck. |
| GET | `/api/tutors` | public | `TutorSearchQuery` from query string — paged listing for the find-tutor UI. |
| GET | `/api/tutors/{id}` | public | Full `TutorProfileResponse` (includes recent reviews). |
| GET | `/api/tutors/{id}/reviews` | public | Paged reviews via `TutorReviewsQuery`. |
| POST | `/api/classes` | `[Authorize]` | Create class. Role-based student resolution: `Student` ignores body `StudentId`, `Parent` must own the student, `Admin` pass-through. |
| GET | `/api/classes/{id}` | `[Authorize]` | Class detail. Caller must be the tutor, the student, the student's parent, or Admin. |

### Tutor search query (`GET /api/tutors`)

`TutorSearchQuery` (FromQuery, validated by `TutorSearchQueryValidator`):

| Param | Type | Notes |
| --- | --- | --- |
| `search` | string | Case-insensitive match against tutor full name or any subject name. |
| `subject` | string | Vietnamese subject name from `Subjects.SubjectName` (e.g. `Toán`). Frontend sentinel `Tất cả` must be omitted, not sent as the literal. |
| `type` | `all` \| `tutor` \| `teacher` | Default `all`. Maps to `Tutor.TutorType`. |
| `minPrice` | int (VND) | Default `0`. Inclusive. |
| `maxPrice` | int (VND) | Default `500000`. Inclusive. |
| `page` | int | 1-based. Default `1`. Server page size is fixed at `10` (see `TutorService.PageSize`). |

Response is `ApiResponse<PagedResult<TutorListingResponse>>`. Validation failures flow through `ValidationException` → `GlobalExceptionHandlerMiddleware`.

## Branching & PRs

Default base for PRs is `dev` (not `main`). CodeRabbit auto-reviews PRs targeting `main` or `dev` (see `.coderabbit.yaml`).
