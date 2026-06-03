# Local Database Configuration

AI Readiness Consultant Hub uses PostgreSQL only. SQLite is not supported at runtime.

Do not commit database URLs, passwords, or local appsettings files. `appsettings.Development.json` and `appsettings.Local.json` are ignored by Git.

## Option A - Environment Variable

Run the app from the same terminal where the PostgreSQL connection string is exported:

```bash
export ConnectionStrings__DefaultConnection="Host=...;Port=6543;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=60;Command Timeout=60"
dotnet run --urls http://127.0.0.1:5091
```

## Option B - User Secrets

Use ASP.NET Core user secrets for local development:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Port=6543;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true;Pooling=false;Timeout=60;Command Timeout=60"
dotnet run --urls http://127.0.0.1:5091
```

## Startup Migrations

Migrations run on startup by default:

```text
RunMigrationsOnStartup=true
```

To start the app without applying migrations, set:

```bash
export RunMigrationsOnStartup=false
```

When migrations are disabled, seed data is skipped too. Use this only for diagnostics; deployment still requires the migrations to be valid.
