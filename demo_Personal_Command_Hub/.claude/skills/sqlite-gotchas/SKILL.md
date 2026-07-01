---
name: sqlite-gotchas
description: EF Core + SQLite pitfalls and migration rules for PCH. Load before writing EF queries, adding/ordering by date fields, or running dotnet ef migrations in PCH.Data.
---

# SQLite Gotchas — Personal Command Hub

The database is SQLite via EF Core (`PchDbContext`, file `pch.db`, gitignored). These issues compile cleanly and only surface at runtime, so they're easy to ship by accident.

## 1. No `ORDER BY` on `DateTimeOffset`
SQLite throws `NotSupportedException: SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses`. The entities use `DateTimeOffset` widely (`TaskItem.DueDate/CreatedAt/UpdatedAt`, `Booking.StartTime/EndTime`, `NewsItem.Published/FetchedAt`).

- **Fix:** materialize with `await ...ToListAsync(ct)` first, then `OrderBy`/`ThenBy` in memory (LINQ to Objects).
- The same applies to other unsupported translations — when in doubt, sort/group client-side after a simple query.
- It's fine to **filter** (`Where`) on these in the DB; it's specifically ORDER BY (and some aggregates) that break.

## 2. When NOT to create a new migration
The `InitialCreate` migration already includes columns people assume are "new" — e.g. `Progress`, `Category`, `DueDate` on `tasks` were all there from Day 1. **Check before generating a migration:**

- Read `PCH.Data/Migrations/PchDbContextModelSnapshot.cs` and confirm whether the column/table already exists.
- Run `dotnet ef migrations add <Name>` only if the entity model has genuinely diverged from the snapshot. If nothing changed, EF produces an empty `Up`/`Down` migration — delete it; don't commit no-op migrations.
- A model change with no migration is the real bug to watch for — not a missing duplicate.

## 3. Migration workflow (full dotnet path)
`dotnet` is not on PATH here. From the repo root:
```
& "C:\Program Files\dotnet\dotnet.exe" ef migrations add <Name> --project PCH.Data --startup-project PCH.Api
& "C:\Program Files\dotnet\dotnet.exe" ef database update --project PCH.Data --startup-project PCH.Api
```
The API also calls `db.Database.Migrate()` on startup, so pending migrations apply automatically when it runs.

## 4. Conventions to preserve
- Tables are plural snake_case (`tasks`, `news_items`, `bookings`) via `ToTable(...)`.
- `pch.db`, `*.db-shm`, `*.db-wal` are gitignored — local data never goes in git, so feel free to wipe/reseed during testing.

## Always verify at runtime
None of the above fails the build. After any query or migration change, run the API and exercise the endpoint (a GET that orders results is the canonical trip-wire for issue #1).
