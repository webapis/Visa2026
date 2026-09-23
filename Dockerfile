# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
# Note: omitting "# syntax=docker/dockerfile:1.4" avoids a Docker Hub pull of the Dockerfile frontend (needed on restricted networks).
#
# NuGet: RUN lines use BuildKit cache mounts (id=visa2026-nuget) so packages persist across docker builds on this machine.
# Runtime apt/fonts/LibreOffice live in webapia/visa2026-runtime-base (docker/Dockerfile.runtime-base) so routine
# app publishes only restore + publish + copy. Requires BuildKit (on by default in Docker Desktop).

ARG RUNTIME_BASE_IMAGE=webapia/visa2026-runtime-base:v1
FROM ${RUNTIME_BASE_IMAGE} AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /src
ARG APP_VERSION="unknown"
ARG GIT_SHA="unknown"

# Install DevExpress license so build tools don't emit DX1000/DX1001 warnings
RUN mkdir -p /root/.config/DevExpress
COPY DevExpress.Key/DevExpress_License.txt /root/.config/DevExpress/DevExpress_License.txt
COPY DevExpress.Key/DevExpress_License.txt ./DevExpress.Key/DevExpress_License.txt

COPY ["Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj", "Visa2026.Blazor.Server/"]
COPY ["Visa2026.Module/Visa2026.Module.csproj", "Visa2026.Module/"]

RUN --mount=type=cache,id=visa2026-nuget,target=/root/.nuget/packages \
    dotnet restore "Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj"

COPY . .

WORKDIR "/src/Visa2026.Blazor.Server"
# Publish only (no separate dotnet build) - publish compiles once.
RUN --mount=type=cache,id=visa2026-nuget,target=/root/.nuget/packages \
    dotnet publish "Visa2026.Blazor.Server.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:NoWarn=DX1000%3BDX1001 \
    /p:InformationalVersion=${APP_VERSION}+${GIT_SHA}

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Entrypoint runs as root to fix volume ownership for DataProtection keys,
# then drops privileges to the built-in `app` user for the actual process.
COPY docker/visa2026-entrypoint.sh /app/visa2026-entrypoint.sh
RUN sed -i 's/\r$//' /app/visa2026-entrypoint.sh && chmod +x /app/visa2026-entrypoint.sh

USER root
ENTRYPOINT ["/app/visa2026-entrypoint.sh"]
