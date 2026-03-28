# EF Core (SQLite) + Hexagonal + Email VO + UoW Sample

## Prereqs
- .NET 8 SDK
- dotnet-ef tool (once):
  - `dotnet tool install --global dotnet-ef`

## Restore & Run
- `dotnet restore`
- `dotnet run --project src/MyApp`

## Create migrations (SQLite)
From repo root:
- `dotnet ef migrations add InitialCreate --project src/MyApp --startup-project src/MyApp --output-dir Adapters/Database/Migrations`
- `dotnet ef database update --project src/MyApp --startup-project src/MyApp`

## Notes
- `NormalizedEmail` is stored as `Email.ToUpperInvariant()` and has a UNIQUE index for stable duplicate detection.
- Registration is insert-only; duplicates are detected by DB unique constraint and mapped to `UserAlreadyExistsException`.
- Transaction boundary is controlled in `RegistrationService` via `IUnitOfWorkPort`.
