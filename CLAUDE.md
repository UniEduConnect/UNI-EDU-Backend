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

## Endpoints

### `GET /api/tutors` — public tutor search

Powers the find-tutor UI shared by [FindTutor.tsx](../UNI-EDU-Frontend-V2/src/pages/FindTutor.tsx), [StudentFindTutor.tsx](../UNI-EDU-Frontend-V2/src/pages/student/StudentFindTutor.tsx), and [ParentFindTutor.tsx](../UNI-EDU-Frontend-V2/src/pages/parent/ParentFindTutor.tsx). Public — no `[Authorize]`. Route follows the `api/[controller]` convention used by `UsersController`, so the path the frontend should call is `/api/tutors` (not the bare `/tutors`).

**Query (all optional, `[FromQuery]` bound to `TutorSearchQuery`)**:

| Param      | Type                          | Notes                                                                                                  |
| ---------- | ----------------------------- | ------------------------------------------------------------------------------------------------------ |
| `search`   | string                        | Case-insensitive match against tutor full name **or** any of their subject names.                       |
| `subject`  | string                        | Vietnamese subject name as stored in `Subjects.SubjectName` (e.g. `Toán`). Frontend sentinel `Tất cả` must be sent as omitted, not as the literal. |
| `type`     | `all` \| `tutor` \| `teacher` | Default `all`. Maps to a new `Tutor.TutorType` enum (see below).                                       |
| `minPrice` | int (VND)                     | Default `0`. Inclusive.                                                                                |
| `maxPrice` | int (VND)                     | Default `500000` (matches the slider cap in `FindTutor.tsx`). Inclusive.                               |
| `page`     | int                           | 1-based. Default `1`. Server page size is fixed at `10` to match the frontend's `pageSize`.            |

Validator (`TutorSearchQueryValidator`): `page >= 1`, `minPrice >= 0`, `maxPrice >= minPrice`, `type` ∈ allowed set. Failures surface as `ValidationException` through the existing pipeline.

**Response** — wrap in `ApiResponse<PagedResult<TutorListingResponse>>`:

```jsonc
{
  "statusCode": 200,
  "message": "Get tutors successfully",
  "data": {
    "items": [ /* TutorListingResponse[] */ ],
    "total": 124,        // total after filters, before paging — frontend uses this for the result count + pageCount
    "page": 1,
    "pageSize": 10
  }
}
```

`PagedResult<T>` is generic and reusable — put it in `UNI-EDU-Backend.API/Commons/PagedResult.cs` next to `ApiResponse`.

**`TutorListingResponse` shape** (mirror the frontend `TutorListing` interface in [StudentContext.tsx:70-91](../UNI-EDU-Frontend-V2/src/contexts/StudentContext.tsx#L70-L91) exactly — field names are camelCased on the wire by the default JSON serializer):

```csharp
public class TutorListingResponse
{
    public Guid Id { get; set; }                       // Tutor.TutorID
    public string Name { get; set; }                   // Tutor.FullName
    public string Avatar { get; set; }                 // Tutor.AvatarUrl (new)
    public List<string> Subjects { get; set; }         // names via Tutor.Subjects M2M (new)
    public float Rating { get; set; }                  // Tutor.AverageRating (existing)
    public int TotalReviews { get; set; }              // COUNT(Reviews where TutorID = ...)
    public int TotalSessions { get; set; }             // COUNT(ClassSessions where TutorID = ...)
    public int YearsExperience { get; set; }           // Tutor.YearsExperience (new int; keep the existing free-text Experience separately or migrate it)
    public int HourlyRate { get; set; }                // Tutor.HourlyRate (new, VND)
    public string Location { get; set; }               // Tutor.Location (new) — keep Address for billing
    public bool Verified { get; set; }                 // Tutor.IsVerified (new)
    public string Bio { get; set; }                    // Tutor.Bio (existing)
    public string School { get; set; }                 // Tutor.School (new)
    public string Degree { get; set; }                 // Tutor.Degree (existing)
    public string Type { get; set; }                   // "tutor" | "teacher" — serialize Tutor.TutorType as lowercase string (the frontend discriminator is lowercase)
    public List<AvailableSlotDto> AvailableSlots { get; set; }  // { day, time }[], nullable
    public List<string> Certificates { get; set; }     // nullable
    public string IntroVideoUrl { get; set; }          // nullable
    public string TeachingStyle { get; set; }          // nullable
    public List<string> Achievements { get; set; }     // nullable
}
```

`AvailableSlot.day` values are Vietnamese (`Thứ 2`, `Chủ nhật`, …) — keep them as raw strings; the frontend renders them verbatim.

**Required domain changes** — the current [Tutor.cs](UNI-EDU-Backend.Domain/Models/Tutor.cs) only carries `FullName`, `DateOfBirth`, `Gender`, `Address`, `Degree`, `Experience`, `Bio`, `AverageRating`. Add (one migration, named e.g. `AddTutorListingFields`):

- Scalar columns on `Tutor`: `AvatarUrl`, `Location`, `School`, `HourlyRate` (int), `YearsExperience` (int), `IsVerified` (bool), `TeachingStyle`, `IntroVideoUrl`, `TutorType` (new enum `TutorType { Tutor, Teacher }` under `Domain/Enums/`).
- Collection columns on `Tutor` for `Certificates`, `Achievements`, and `AvailableSlots`. Postgres `text[]`/`jsonb` via Npgsql is the path of least resistance — use `jsonb` for `AvailableSlots` (it's a list of objects) and `text[]` for the two string lists. Configure in `OnModelCreating`, **not** via annotations (per the existing convention).
- M2M `Tutor` ↔ `Subject` via a join entity `TutorSubject { TutorID, SubjectID }` with a composite key configured in `OnModelCreating`. Add `ICollection<Subject> Subjects` to `Tutor` and the inverse on `Subject`.

`TotalReviews` and `TotalSessions` are **derived** from `Reviews` and `ClassSessions` — do not cache them on `Tutor` unless a later perf issue demands it.

**Wiring (follow the existing UserService/UserRepository split)**:

1. `Application/Interfaces/ITutorRepository.cs` — single method `Task<(IReadOnlyList<Tutor> Items, int Total)> SearchAsync(TutorSearchQuery query, CancellationToken ct)`. Pass paging into the repo so it can `Skip/Take` after `CountAsync`, both inside one query plan.
2. `Infrastructure/Repositories/TutorRepository.cs` — build the `IQueryable<Tutor>` with `.Include(t => t.Subjects)`, apply filters conditionally (`if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(...)`), `EF.Functions.ILike` for case-insensitive matching on Postgres, then project review/session counts via correlated subqueries (`Reviews.Count(r => r.TutorID == t.TutorID)`) so paging doesn't pull every row.
3. `Application/Services/Tutors/ITutorService.cs` + `TutorService.cs` — orchestrate, then `Adapt<TutorListingResponse>()` via Mapster (register a `TypeAdapterConfig` for `Tutor.TutorType` → lowercase string and for the M2M → `List<string>` of subject names).
4. `API/Controllers/TutorsController.cs` — single `[HttpGet]` action taking `[FromQuery] TutorSearchQuery`, wraps in `ApiResponse<PagedResult<TutorListingResponse>>`. No `try/catch` — let `GlobalExceptionHandlerMiddleware` handle it.
5. Register both `ITutorService` and `ITutorRepository` in `Program.cs` alongside the existing `IUserService`/`IUserRepository` lines.

**Pattern choice** — `UsersController` calls `IUserService` directly; the MediatR pipeline (with `ValidationBehavior<,>`) is registered but unused by it. For this endpoint either pattern is acceptable, but **prefer MediatR** (`GetTutorListingsQuery : IRequest<PagedResult<TutorListingResponse>>` + handler in `Application/Features/Tutors/Queries/`) so the validator runs automatically. If you stick with the direct service style, invoke `IValidator<TutorSearchQuery>` manually in the controller before calling the service — don't let invalid input reach the repository.

**Error cases** — `BadRequestException` for nothing meaningful here (validator covers shape); return an empty `items` list with `total = 0` when filters match nothing (the frontend already renders an empty-state message). Do not 404.

## Branching & PRs

Default base for PRs is `dev` (not `main`). CodeRabbit auto-reviews PRs targeting `main` or `dev` (see `.coderabbit.yaml`).
