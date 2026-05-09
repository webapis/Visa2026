# NuGet cache mount (id=visa2026-nuget-importer) persists packages across importer image builds. Same BuildKit notes as root Dockerfile.
# Note: omitting "# syntax=docker/dockerfile:1.4" avoids a Docker Hub pull of the Dockerfile frontend (needed on restricted networks).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install DevExpress license so build tools don't emit DX1000/DX1001 warnings
RUN mkdir -p /root/.config/DevExpress
COPY DevExpress.Key/DevExpress_License.txt /root/.config/DevExpress/DevExpress_License.txt

COPY ["Visa2026.DataImporter/Visa2026.DataImporter.csproj", "Visa2026.DataImporter/"]
RUN --mount=type=cache,id=visa2026-nuget-importer,target=/root/.nuget/packages \
    dotnet restore "Visa2026.DataImporter/Visa2026.DataImporter.csproj"
COPY . .
WORKDIR "/src/Visa2026.DataImporter"
RUN --mount=type=cache,id=visa2026-nuget-importer,target=/root/.nuget/packages \
    dotnet publish "Visa2026.DataImporter.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:NoWarn=DX1000%3BDX1001

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Visa2026.DataImporter.dll"]
