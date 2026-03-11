# Deployment Documentation

This document outlines the steps to deploy the Visa2026 application using Docker.

## Prerequisites

Refer to the [Connection String Management](./CONNECTION_STRINGS.md) document for detailed information on configuring database connections for different environments.

*   Docker Desktop installed and running.

## Docker Configuration

The application is dockerized using a multi-stage Dockerfile and docker-compose to run with a Microsoft SQL Server database.

### Dockerfile

The `Dockerfile` is located at the root of the project and defines the steps to build the Docker image for the Visa2026.Blazor.Server application. It uses a multi-stage build process:

1.  **Base Stage**: Uses the `mcr.microsoft.com/dotnet/aspnet:8.0` image as the base for the final runtime image.
2.  **Build Stage**: Uses the `mcr.microsoft.com/dotnet/sdk:8.0` image to build the application. It copies the necessary `.csproj` files, restores NuGet packages, and builds the Visa2026.Blazor.Server project.
3.  **Publish Stage**: Publishes the built application to a directory.
4.  **Final Stage**: Copies the published output from the publish stage to the base runtime image and sets the entry point for the application. It also installs native dependencies required for graphics rendering.

```dockerfile
#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

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

# Switch to root user to install dependencies
USER root

# Force apt to use HTTPS sources
RUN echo "deb https://deb.debian.org/debian bookworm main" > /etc/apt/sources.list && \
    echo "deb https://deb.debian.org/debian-security/ bookworm-security main" >> /etc/apt/sources.list && \
    echo "deb https://deb.debian.org/debian bookworm-updates main" >> /etc/apt/sources.list

# Install SkiaSharp dependencies
RUN apt-get update && apt-get install -y libfontconfig1 libexpat1 && apt-get clean && rm -rf /var/lib/apt/lists/*

# Switch back to the default app user
USER app

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

The `docker-compose.yml` file defines the services that make up the application: the Blazor application and a SQL Server Express database.

```yaml
services:
  app:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=Visa2026Db;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True
    depends_on:
      - sqlserver

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SA_PASSWORD}
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

volumes:
  sqlserver_data:
```

### .env

The `.env` file stores sensitive environment variables, such as the database password.

```
SA_PASSWORD=your_strong_password
```

## Deployment Steps

1.  **Edit `.env`**: Open the `.env` file and replace `your_strong_password` with an actual strong password for `SA_PASSWORD`.
2.  **Build and Run**: Navigate to the project root in the terminal (where `docker-compose.yml` is located) and run:

    ```sh
    docker compose up --build
    ```

    This command will:
    *   Build your application's Docker image (if it hasn't been built or if changes were made to the `Dockerfile`).
    *   Create and start the `sqlserver` and `app` containers.

Your application should then be accessible at `http://localhost:8080`. You can also connect to your SQL Server at `localhost:1433`.
