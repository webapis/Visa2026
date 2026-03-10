# Deployment Documentation

This document outlines the steps to deploy the Visa2026 application using Docker.

## Prerequisites

*   Docker Desktop installed and running.

## Docker Configuration

The application is dockerized using a multi-stage Dockerfile and docker-compose.

### Dockerfile

The `Dockerfile` is located at the root of the project and defines the steps to build the Docker image for the Visa2026.Blazor.Server application. It uses a multi-stage build process:

1.  **Base Stage**: Uses the `mcr.microsoft.com/dotnet/aspnet:8.0` image as the base for the final runtime image.
2.  **Build Stage**: Uses the `mcr.microsoft.com/dotnet/sdk:8.0` image to build the application. It copies the necessary `.csproj` files, restores NuGet packages, and builds the Visa2026.Blazor.Server project.
3.  **Publish Stage**: Publishes the built application to a directory.
4.  **Final Stage**: Copies the published output from the publish stage to the base runtime image and sets the entry point for the application.

```dockerfile
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj", "Visa2026.Blazor.Server/"]
COPY ["Visa2026.Module/Visa2026.Module.csproj", "Visa2026.Module/"]
RUN dotnet restore "Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj"
COPY . .
WORKDIR "/src/Visa2026.Blazor.Server"
RUN dotnet build "Visa2026.Blazor.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Visa2026.Blazor.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Visa2026.Blazor.Server.dll"]
```

### .dockerignore

The `.dockerignore` file is located at the root of the project and specifies files and directories that should be excluded from the Docker build context. This helps to speed up the build process and reduce the final image size.

```
**/.classpath
**/.dockerignore
**/.env
**/.git
**/.gitignore
**/.project
**/.settings
**/.toolstarget
**/.vs
**/.vscode
**/*.*proj.user
**/*.dbmdl
**/*.jfm
**/azds.yaml
**/bin
**/charts
**/docker-compose*
**/Dockerfile*
**/node_modules
**/npm.config
**/obj
**/packages
**/secrets.dev.yaml
**/secrets.yaml
**/Values.dev.yaml
**/Values.yaml
LICENSE
README.md
```

### docker-compose.yml

The `docker-compose.yml` file defines the services that make up the application: the Blazor application, a PostgreSQL database, and a SQL Server Express database.

```yaml
version: '3.8'

services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=Visa2026Db;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True
      - ConnectionStrings__PostgresConnection=Host=postgres;Database=postgresdb;Username=postgres;Password=${POSTGRES_PASSWORD}
    depends_on:
      - sqlserver
      - postgres

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SA_PASSWORD}
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

  postgres:
    image: postgres:latest
    environment:
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
      - POSTGRES_USER=postgres
      - POSTGRES_DB=postgresdb
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  sqlserver_data:
  postgres_data:
```

### .env

The `.env` file stores sensitive environment variables, such as database passwords.  **Important:** Replace `your_strong_password` with a real, strong password.

```
SA_PASSWORD=your_strong_password
POSTGRES_PASSWORD=your_strong_password
```

## Deployment Steps

1.  **Edit `.env`**: Open the `.env` file and replace `your_strong_password` with actual strong passwords for both `SA_PASSWORD` and `POSTGRES_PASSWORD`.
2.  **Build and Run**: Navigate to the project root in the terminal (where `docker-compose.yml` is located) and run:

    ```sh
    docker compose up --build
    ```

    This command will:
    *   Build your application's Docker image (if it hasn't been built or if changes were made to the `Dockerfile`).
    *   Create and start the `sqlserver`, `postgres`, and `app` containers.

Your application should then be accessible at `http://localhost:8080`. You can also connect to your SQL Server at `localhost:1433` and PostgreSQL at `localhost:5432`.
