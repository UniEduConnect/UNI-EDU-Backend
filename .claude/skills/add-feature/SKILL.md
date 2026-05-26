---
name: add-feature
description: Scaffold an end-to-end feature in UNI-EDU-Backend following the clean-architecture conventions in CLAUDE.md — Domain entity → DbContext config → EF migration → repository (interface in Application, impl in Infrastructure) → DTOs + FluentValidation validator → service (interface + impl, calls EnsureValidAsync) → controller (ApiResponse<T>, typed exceptions, ReadCallerOrThrow) → Program.cs DI wiring. Invoke when adding a new resource/endpoint group that needs all of these layers.
---

# add-feature

You are scaffolding a new feature across all four projects in `UNI-EDU-Backend.slnx`. The goal: produce code that **already matches the conventions in `CLAUDE.md`** so the user never has to send it back for "please follow the pattern."

## Step 0 — Clarify scope before writing files

Ask the user (concisely) for whatever isn't already obvious from their request:

- **Feature name** (singular noun, PascalCase). e.g. `Subject`, `Lesson`, `Payment`. Folder/namespace pieces derive from this.
- **Endpoints in this slice** — which of `POST`, `GET /{id}`, `GET` (list, paged), `PATCH/PUT`, `DELETE`. Default to just what they asked for; don't invent more.
- **Auth**: public, `[Authorize]` for any logged-in user, or role-restricted? If role-restricted, which of `Admin`/`Tutor`/`Student`/`Parent`?
- **Persistence shape**: is there a new Domain entity, or does it reuse existing ones? If new, ask for the field list (or infer from the user's description and confirm in one line).
- **Relationships**: 1-1, 1-many, many-many to which existing entities? Cascade vs Restrict on delete?
- **jsonb/text[] columns** needed? (Postgres-specific configuration goes in `OnModelCreating`, not data annotations.)

Skip questions whose answer is already in their prompt. Don't ask more than 3 at once.

## Step 1 — Domain layer

Project: **`UNI-EDU-Backend.Domain`**.

- **Entity**: `Domain/Models/<Feature>.cs`. POCO. Use `[Key]` and `[ForeignKey("NavProperty")]` for the simple cases; everything else (cascade, jsonb, text[], M2M join tables) is configured in `OnModelCreating`, **not** with annotations.
- **Enums**: `Domain/Enums/<Name>.cs` for any new closed sets. Existing enums to reuse: `UserRole`, `ClassStatus`, `ClassFormat`, `EscrowStatus`, `WalletTxType`, `SessionStatus`, `TutorType`. Stored as `int` by EF.
- **Navigation properties**: `virtual` collections and references. Initialize collections with `= new()` so service code doesn't NRE.
- **Value objects stored as jsonb**: plain POCO under `Domain/Models/`, no `[Key]`. The DbContext registers the converter.

## Step 2 — Infrastructure: DbContext + migration

Project: **`UNI-EDU-Backend.Infrastructure`**.

1. Add `public DbSet<Feature> Features { get; set; }` to `ApplicationDbContext`.
2. In `OnModelCreating`, add the relationship config near similar entities (group by topic, not by entity). Mirror existing style:
   - 1-1: `HasOne(...).WithOne(...).HasForeignKey<Child>(c => c.Id)`.
   - 1-many: `HasOne(...).WithMany(...).HasForeignKey(...).OnDelete(DeleteBehavior.Restrict|Cascade|SetNull)`. Choose `Restrict` by default when both sides are user-facing aggregates (e.g. Class→Tutor); `Cascade` when the child is purely owned (e.g. Session→Class, ClassMaterial→Class).
   - M2M: explicit join entity (`TutorSubject` is the canonical example) with `HasKey(new { ... })` and `.UsingEntity<Join>(...)`.
   - jsonb: `.HasColumnType("jsonb").HasConversion(serialize, DeserializeJsonList<T>)` — reuse the existing private `DeserializeJsonList<T>` helper at the bottom of `ApplicationDbContext`. **Never** call `JsonSerializer.Deserialize` inline without the empty/null/malformed fallback — empty/`""`/`null` jsonb values must yield an empty list, not throw at materialization.
   - text[]: `.HasColumnType("text[]")` only; no converter.
3. Generate the migration:
   ```powershell
   dotnet ef migrations add Add<Feature> --project UNI-EDU-Backend.Infrastructure --startup-project UNI-EDU-Backend.API
   ```
4. **Read the generated `Up()` and `Down()` before continuing.** Common things to fix by hand:
   - Non-nullable `text[]` columns need `defaultValueSql: "ARRAY[]::text[]"` (otherwise the migration fails on existing rows).
   - Non-nullable `jsonb` columns need `defaultValueSql: "'[]'::jsonb"`.
   - If EF emits a `DropColumn` that another migration already handles, delete the duplicate from both `Up()` and `Down()` and leave a one-line comment pointing at the canonical migration.
5. Apply only when the user confirms — don't run `database update` proactively unless they asked.

## Step 3 — Application: repository interface

Project: **`UNI-EDU-Backend.Application`**.

- File: `Application/Interfaces/Repositories/I<Feature>Repository.cs`.
- Namespace: `UNI_EDU_Backend.Application.Interfaces.Repositories`.
- **Don't** inherit `IGenericRepository<T>` unless every method is trivial CRUD. Most features have at least one query that wants a `.Select` projection, `EF.Functions.ILike`, or a transaction — declare those methods explicitly on the feature-specific interface and keep the data shape close to the data.
- Method signatures take `CancellationToken cancellationToken` as the last parameter. Async methods return `Task<...>`.
- Use the typed response/DTO shapes from `Application/DTOs/<Feature>/` for projection methods; return the Domain entity only when the caller will mutate it.

## Step 4 — Infrastructure: repository implementation

Project: **`UNI-EDU-Backend.Infrastructure`**.

- File: `Infrastructure/Repositories/<Feature>Repository.cs`.
- Constructor takes `ApplicationDbContext` (use primary-constructor syntax for new files: `public class FooRepository(ApplicationDbContext db) : IFooRepository`).
- Conventions:
  - Filter conditionally: `if (!string.IsNullOrWhiteSpace(query.Search)) q = q.Where(...)`.
  - Case-insensitive search uses `EF.Functions.ILike(x.Field, $"%{search}%")` — works on Postgres.
  - For listings, count and page in the same query plan: build the filtered `IQueryable`, `.CountAsync()`, then `.Skip().Take().Select(...).ToListAsync()`.
  - **EF can't translate operations over jsonb-converted lists.** If you need to project from a jsonb column, materialize the raw value first (`.Select(t => new { ... t.WeeklySlots })`) then map client-side after `ToListAsync()`.
  - Multi-step writes that touch multiple tables (e.g. class + wallet debit + transaction + sessions) wrap in `await db.Database.BeginTransactionAsync(ct)` and commit explicitly.
  - **Do not** call `SaveChangesAsync` inside the repository if the service is the unit-of-work boundary — but the existing `ClassRepository` does commit inside transactions, so follow that pattern when the operation is atomically scoped to one method.

## Step 5 — Application: DTOs + validator

- Folder: `Application/DTOs/<Feature>/`.
- Naming: `Create<Feature>Request.cs`, `Update<Feature>Request.cs`, `<Feature>Response.cs`, `<Feature>DetailResponse.cs`, paged list items as `<Feature>ListingResponse.cs`.
- For paged endpoints there's a separate **search-query** DTO bound from `[FromQuery]` — e.g. `<Feature>SearchQuery.cs` with `Page`, plus filter fields. Page size is **fixed server-side at 10** (do not accept `pageSize` from the client) unless the feature spec explicitly says otherwise.
- **Validator** lives next to the DTO: `Create<Feature>RequestValidator.cs : AbstractValidator<Create<Feature>Request>`.
  - It is picked up by `AddValidatorsFromAssembly(applicationAssembly)` in `Program.cs`.
  - It runs **only when the service calls** `await _validator.EnsureValidAsync(request, ct);` — there is no MediatR pipeline. Forgetting that line means validation silently doesn't run.
  - Vietnamese phone number rule (re-use across DTOs): `Matches("^0\\d{9}$")` — starts with `0`, exactly 10 digits, no `+84` form.
  - For Guid required fields: `NotEmpty()` (Guid.Empty is the default and fails this).
  - Paged-query validators: `Page >= 1`, `MinPrice >= 0`, `MaxPrice >= MinPrice`, enum-ish strings against an allow-list.
- Map between DTOs and entities with **Mapster** — call `entity.Adapt<TDto>()` (or `dto.Adapt<TEntity>()`) directly in the service. Don't inject `IMapper` and don't add `CreateMap` lines to `MappingProfile.cs` for new code. If you need a non-default rule (renamed members, ignored properties, computed values), register a `TypeAdapterConfig<TSrc, TDst>.NewConfig()...` in a static initializer under `Application/Mappings/` — but the convention-based map is enough for most DTOs.

## Step 6 — Application: service interface + implementation

- Folder: `Application/Services/<Feature>/`.
- Files: `I<Feature>Service.cs`, `<Feature>Service.cs`.
- Use primary-constructor syntax. Inject the repo, the validators you need (`IValidator<Create<Feature>Request>` etc.), and `IUnitOfWork` if multi-step. **Do not inject `IMapper`** — use Mapster's `.Adapt<>()` extension instead.
- **Order inside a service method**:
  1. Resolve/normalize caller identity (if the method takes `callerUserId` + `callerRole`) — switch on role, set body fields the caller doesn't own (e.g. for `Student` role, override `request.StudentId = callerUserId`), and throw `ForbiddenAccessException` for roles that aren't allowed.
  2. `await _validator.EnsureValidAsync(request, ct);` — **after** identity resolution so we don't validate a request the caller isn't allowed to make at all, but **before** any existence checks.
  3. Existence checks → throw `NotFoundException`.
  4. Business-rule checks → throw `BadRequestException`.
  5. Repo call.
- **Authz on reads**: after fetching the resource, `switch role => allowed?` (see `ClassService.GetClassByIdAsync` for the canonical pattern). Throw `ForbiddenAccessException` on miss.
- **Never** catch the typed exceptions in the service — let them bubble.

## Step 7 — API: controller

Project: **`UNI-EDU-Backend.API`**.

- File: `API/Controllers/<Features>Controller.cs` (plural). Route: `[Route("api/[controller]")]`.
- Use primary-constructor syntax: `public class FeaturesController(IFeatureService svc) : ControllerBase`.
- Every action:
  - Has explicit `[HttpGet]` / `[HttpPost]` / etc. with route template.
  - Returns `Task<IActionResult>`.
  - Wraps the response in `ApiResponse<T>` and uses `StatusCode(StatusCodes.Status..., apiResponse)`.
  - **Does not** `try/catch` — `GlobalExceptionHandlerMiddleware` handles every typed exception from `Application/Exceptions/`.
  - Takes `CancellationToken cancellationToken` as the last parameter and forwards it.
- If the action needs the caller's identity, copy the canonical `ReadCallerOrThrow` helper from `ClassesController` (returns `(Guid UserId, string Role)`). Put it as a `private` method at the bottom of the controller. Use `using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;` at the top so it doesn't collide with `System.UnauthorizedAccessException`.
- Status codes:
  - `POST` create → `201 Created` with the created resource.
  - `GET` single → `200 OK` (the not-found case is the service throwing `NotFoundException`, which the middleware turns into `404`).
  - `GET` list → `200 OK` with `ApiResponse<PagedResult<TItem>>` even when zero results (do **not** 404 an empty list).
- `[Authorize]` per action (not class-level) so you can mix public/private endpoints in one controller. Roles via `[Authorize(Roles = "Admin,Tutor")]` only when the role check is purely existence-based; if it's "must own the resource" then enforce inside the service.

## Step 8 — Program.cs wiring

Add the two `AddScoped` lines next to the existing block — repositories and services are grouped separately:

```csharp
// Repositories
builder.Services.AddScoped<I<Feature>Repository, <Feature>Repository>();

// Services
builder.Services.AddScoped<I<Feature>Service, <Feature>Service>();
```

Mapster needs no per-feature DI registration. If you added a custom `TypeAdapterConfig<TSrc, TDst>.NewConfig()` block, make sure it runs at startup (a static constructor on a `MappingRegistry` class invoked from `Program.cs`, or `TypeAdapterConfig.GlobalSettings.Scan(assembly)` for `IRegister` implementations) — otherwise the runtime falls back to convention-based mapping and your custom rule won't fire.

## Step 9 — Verify before reporting done

1. `dotnet build UNI-EDU-Backend.slnx` — must be clean. File-lock errors on the API DLLs mean the dev server is still running; tell the user to stop it before retrying.
2. If you ran the migration: `dotnet ef database update --project UNI-EDU-Backend.Infrastructure --startup-project UNI-EDU-Backend.API`. Only do this with explicit user confirmation.
3. Don't claim the feature works end-to-end based on the build alone — say "build is clean; please hit the endpoint via Swagger to verify." Build success ≠ behavior correct.

## Anti-patterns to refuse

- ❌ MediatR / `IRequest<T>` / handler classes — the project deliberately removed MediatR; validators are invoked manually.
- ❌ `try/catch` around service calls in controllers — middleware owns this.
- ❌ Data annotations for cascade rules, jsonb, or text[] — those belong in `OnModelCreating`.
- ❌ Catching `JsonException` at the repo or service layer — jsonb fallback already lives in `DeserializeJsonList<T>` in `ApplicationDbContext`.
- ❌ Returning 404 for an empty paged list — return `PagedResult` with `Total = 0`.
- ❌ Inventing endpoints the user didn't ask for. Build what was requested.
- ❌ `Bearer ` prefix in the Swagger Authorize box — the user pastes the raw JWT only; the security definition adds the prefix.
- ❌ Accepting `pageSize` from the client unless the spec explicitly requires it. Fixed at 10.
- ❌ Phone regex with `+84` — Vietnam-only `^0\d{9}$`.

## Output discipline

- After scaffolding, give the user a short list of files created/modified (one path per line, no per-file commentary).
- Mention any manual follow-ups: "I generated the migration but didn't apply it" / "Add a `CreateMap` line to `MappingProfile.cs` if you want Auto-mapping from X to Y".
- Do not write a long summary of what the code does — the diff and `CLAUDE.md` already explain it.
