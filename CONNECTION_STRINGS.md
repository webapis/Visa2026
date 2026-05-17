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

## 2. Switching Databases via Launch Profiles (Visual Studio)

The easiest way to switch between different database targets while developing in Visual Studio is to use the **Launch Profiles** dropdown located next to the green "Start" button.

Two specific profiles are configured in `launchSettings.json`:

*   **Visa2026 - LocalDB**: 
    *   **Target**: The lightweight SQL instance installed with Visual Studio (`(localdb)\mssqllocaldb`).
    *   **Usage**: Best for quick development without needing Docker running.
    *   **Configuration**: Overrides `DefaultConnection` via environment variables to point to the local instance.

*   **Visa2026 - Docker SQL**:
    *   **Target**: The SQL Server container defined in your `docker-compose.yml` (usually `127.0.0.1,1433`).
    *   **Usage**: Best for testing against a "production-like" SQL Server environment or when working with Docker tools.
    *   **Security Note**: This profile is designed to work with **User Secrets**. To securely store your Docker SQL credentials locally without committing them to the repository, run the following command from the `Visa2026.Blazor.Server` project directory:

      ```bash
      dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1,1433;Database=Visa2026DbDev;User Id=sa;Password=YOUR_ACTUAL_SECURE_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
      ```

      *Replace `YOUR_ACTUAL_SECURE_PASSWORD` with the password from your `.env.dev` file.*

### How to use:
1.  In Visual Studio, click the small arrow next to the **Start** button.
2.  Select your desired profile from the list.
3.  Press **F5** to run.

---

## 3. Docker (Release) Environment

When the application is deployed as a production or staging container using the root `docker-compose.yml` file, the connection string is injected as an environment variable. This value overrides any setting present in the `appsettings.json` file inside the container.

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

## 4. E2E Testing (EasyTest) Environment

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