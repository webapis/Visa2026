# Connection String Management

Visa2026 uses **PostgreSQL only** for the application database (EF Core + Npgsql). Legacy **VISA2015** (SQL Server) remains the read-only import source — it is not a Visa2026 database.

The application reads **`DefaultConnection`** from configuration (`Startup.cs`), with an older `ConnectionString` key as a fallback for local overrides.

## Canonical connection string shape

```text
Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=…;Persist Security Info=True;EFCoreProvider=Postgres
```

- Include **`EFCoreProvider=Postgres`** (or use a native Npgsql `Host=` string without `Server=` / `Data Source=`).
- SQL Server / LocalDB connection strings are **rejected** at startup (`DatabaseProviderDetector.ConfigureEfCore`).

## 1. Local development (F5)

Defaults live in:

- `Visa2026.Blazor.Server/appsettings.json`
- `Visa2026.Blazor.Server/appsettings.Development.json`

Launch profile **`Visa2026 - PostgreSQL`** in `Properties/launchSettings.json` overrides `DefaultConnection` the same way (local DB `visa2026`).

Prerequisites: PostgreSQL listening on `localhost:5432`, empty or existing DB `visa2026`, credentials matching appsettings (local default password in repo is for workstation use only — never use it in prod).

### Visual Studio

1. Select **Visa2026 - PostgreSQL** (or **IIS Express**, which uses appsettings defaults).
2. Press **F5**.

Optional: store a non-default password via user-secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=YOUR_PASSWORD;Persist Security Info=True;EFCoreProvider=Postgres"
```

## 2. Docker

Compose stacks inject `ConnectionStrings__DefaultConnection` pointing at the **`postgres`** service (not SQL Server). Example shape:

```yaml
environment:
  - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=${DB_NAME};Username=${PG_USER};Password=${PG_PASSWORD};Persist Security Info=True;EFCoreProvider=Postgres
```

See `docker-compose.dev.yml` / `docker-compose.prod.yml` and `.env.dev.example`. Secrets come from the env file (`PG_PASSWORD`, etc.).

## 3. EasyTest (E2E)

Isolated catalog **`visa2026_easytest`** on the same Postgres instance:

- `Visa2026.Blazor.Server/appsettings.EasyTest.json`
- EasyTest host env from `Visa2026.E2E.Tests` (`EasyTestHostEnvironment`)

Do not point EasyTest at the F5 `visa2026` database.

## 4. IIS on-prem slots

Each slot env file (`C:\visa2026\env\prod.env`, `staging.env`, `demo.env`) must set:

- `EFCORE_PROVIDER=Postgres`
- `PG_HOST`, `PG_PORT`, `PG_USER`, `PG_PASSWORD`, `DB_NAME` (`visa2026_prod` / `visa2026_staging` / `visa2026_demo`)

`Configure-Visa2026Production.ps1` writes the Npgsql connection string into `appsettings.Production.json`. See [docs/ON_PREM_WINDOWS_IIS.md](docs/ON_PREM_WINDOWS_IIS.md).

## 5. What is not supported

- SQL Server Express / LocalDB as a Visa2026 app database
- Restoring a SQL `.bak` into Postgres (use empty PG + `--import-visa2014` from VISA2015)