# Connection String Management

This document explains how the database connection string is managed in different environments: local development and Docker-based deployment.

The application is designed to be flexible, using a fallback mechanism in `Startup.cs` to read the connection string from the configuration.

## Key Naming Convention

The standard and primary key used for the application's database connection is **`DefaultConnection`**.

While the application has fallback logic to support an older `ConnectionString` key for backward compatibility during local development, all new configurations, including Docker environments and local `appsettings.json` files, should use `DefaultConnection`.

## 1. Local Development Environment

For local development, the connection string is defined in the `appsettings.json` or `appsettings.Development.json` file located within the `Visa2026.Blazor.Server` project.

The startup code is configured to look for the connection string under two possible keys in order:
1.  `DefaultConnection` (Primary)
2.  `ConnectionString` (Fallback)

This provides flexibility during local development.

### Example `appsettings.json` for Local Development

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  // ... other settings
}
```

When you run the application locally from Visual Studio, it will use this string to connect to your local SQL Server instance (e.g., `(localdb)\mssqllocaldb`).

## 2. Docker (Release) Environment

When the application is deployed using Docker via the `docker-compose.yml` file, the connection string is **injected as an environment variable**. This value overrides any setting present in the `appsettings.json` file inside the container.

### `docker-compose.yml` Configuration

The `docker-compose.yml` file defines the connection string for the `app` service:

```yaml
services:
  app:
    # ...
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=Visa2026Db;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True
    # ...
```

### How it Works

*   **`ConnectionStrings__DefaultConnection`**: The double underscore (`__`) is the standard .NET convention for mapping an environment variable to a nested JSON configuration key (`ConnectionStrings:DefaultConnection`).
*   **`Server=sqlserver`**: The server name `sqlserver` directly corresponds to the name of the SQL Server service defined in the same `docker-compose.yml` file. Docker's internal networking resolves this service name to the correct container's IP address.
*   **`${SA_PASSWORD}`**: The database password is not hard-coded. It is dynamically loaded from the `.env` file at the root of the project, keeping secrets separate from the configuration.

This setup ensures that the application container can communicate with the database container within the isolated Docker network without requiring code changes.

## 3. E2E Testing (EasyTest) Environment

For running automated end-to-end (E2E) UI tests with DevExpress EasyTest, a separate connection string named `EasyTestConnectionString` is used.

### Purpose

The primary reason for a dedicated test connection string is to **isolate the test environment**. Automated tests often create, modify, and delete data. Using a separate database (e.g., `Visa2026EasyTest`) ensures that test runs do not corrupt or interfere with the local development database.

### Configuration

This connection string is also configured in the `appsettings.json` file.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "EasyTestConnectionString": "Server=(localdb)\\mssqllocaldb;Database=Visa2026EasyTest;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  // ...
}
```

When you run the E2E tests, the EasyTest framework is configured to look for and use this specific connection string to set up and interact with the test database.