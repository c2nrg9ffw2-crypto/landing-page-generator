---
name: dotnet-patterns
description: Project conventions for .NET 8 Web API + EF Core code in PCH. Load when writing or reviewing controllers, services, DTOs, entities, DbContext, migrations, or Program.cs in PCH.Api/PCH.Core/PCH.Data.
---

# .NET Patterns — Personal Command Hub

Conventions for backend code in this solution. Follow these without re-deriving them.

## Layering
- **No business logic in controllers.** Controllers bind input, call a service/DbContext, map to a DTO, and return an `ActionResult`. Keep reconciliation/validation rules in private helpers or services so create and update paths can't diverge (e.g. `ApplyProgressStatusSync` in `TasksController`).
- **Records for DTOs, classes for EF entities.** DTOs live in `PCH.Core/Dtos`. Entities live in `PCH.Core/Models`.
- **DTOs are positional records** consumed positionally by the Blazor client (`new TaskCreateDto(title, desc, ...)`). If you change a DTO's parameter order, update every call site — the compiler won't catch a reordered-but-same-type mismatch.

## Async & DI
- **Async everywhere for IO.** Every controller action and data method is `async`, returns `Task`/`Task<T>`, takes a `CancellationToken`, and uses EF's async APIs (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`).
- **Inject, never `new`.** Use constructor injection (`PchDbContext`, services, `HttpClient`). Register everything in `Program.cs`.
- **XML doc comments on all public methods**, including DTO record parameters (`<param>`).

## Database conventions
- Table names are **plural snake_case** (`tasks`, `news_items`, `bookings`), configured via `ToTable(...)` in `PchDbContext.OnModelCreating`.
- `Title` columns are `IsRequired().HasMaxLength(...)`. Mirror that limit in the DTO's `[StringLength]`.

## Validation gotcha (positional records)
Put validation attributes **directly on the constructor parameter**, NOT with the `[property:]` target:

```csharp
// CORRECT — validation runs
public record TaskCreateDto(
    [Required(AllowEmptyStrings = false)] [StringLength(256)] string Title, ...);

// WRONG — ASP.NET ignores it and throws InvalidOperationException at request time
public record TaskCreateDto(
    [property: Required] string Title, ...);
```
With `[ApiController]`, valid attributes auto-return `400` on bad input — no manual checks needed.

## SQLite + DateTimeOffset (runtime-only failure)
SQLite **cannot `ORDER BY` a `DateTimeOffset`** — it throws `NotSupportedException` at runtime, not compile time. The `TaskItem` fields `DueDate`/`CreatedAt`/`UpdatedAt` are all `DateTimeOffset`.

```csharp
// WRONG — compiles, 500s at runtime
await _db.Tasks.OrderBy(t => t.DueDate).ToListAsync(ct);

// CORRECT — materialize, then order in memory (LINQ to Objects)
var tasks = await _db.Tasks.ToListAsync(ct);
var ordered = tasks.OrderBy(t => t.Status).ThenBy(t => t.DueDate ?? DateTimeOffset.MaxValue);
```
See the `sqlite-gotchas` skill for more.

## Secrets
Never hardcode credentials or put them in `appsettings.json`. Use `appsettings.*.local.json` or `dotnet user-secrets` (both gitignored).

## Build/run in this environment
`dotnet` is not on PATH. Use `& "C:\Program Files\dotnet\dotnet.exe" build PCH.sln`. To run a project for HTTP testing, set `$env:ASPNETCORE_URLS="http://localhost:5292"` (Api) / `5290` (App) first so `UseHttpsRedirection` doesn't 307-redirect cross-process calls. **Always run a real build + smoke test — a green compile hides SQLite/validation runtime bugs.**
