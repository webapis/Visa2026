# syntax=docker/dockerfile:1.4
# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
#
# NuGet: RUN lines use BuildKit cache mounts (id=visa2026-nuget) so packages persist across docker builds on this machine.
# The first build still downloads everything once; later builds reuse the cache when package references are unchanged.
# Requires BuildKit (on by default in Docker Desktop). Clearing "docker builder cache" removes this; avoid on metered links until a Wi-Fi build refills it.

# Ubuntu jammy: apt uses archive.ubuntu.com — often works when deb.debian.org (Bookworm) is blocked on the network.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install DevExpress license so build tools don't emit DX1000/DX1001 warnings
RUN mkdir -p /root/.config/DevExpress
COPY DevExpress.Key/DevExpress_License.txt /root/.config/DevExpress/DevExpress_License.txt

# Copy the runtime license key file into the source directory
COPY DevExpress.Key/DevExpress_License.txt ./DevExpress.Key/DevExpress_License.txt

COPY ["Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj", "Visa2026.Blazor.Server/"]
COPY ["Visa2026.Module/Visa2026.Module.csproj", "Visa2026.Module/"]

RUN --mount=type=cache,id=visa2026-nuget,target=/root/.nuget/packages \
    dotnet restore "Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj"

COPY . .

WORKDIR "/src/Visa2026.Blazor.Server"
RUN --mount=type=cache,id=visa2026-nuget,target=/root/.nuget/packages \
    dotnet build "Visa2026.Blazor.Server.csproj" -c Release -o /app/build /p:NoWarn=DX1000%3BDX1001

FROM build AS publish
ARG APP_VERSION="unknown"
ARG GIT_SHA="unknown"
WORKDIR "/src/Visa2026.Blazor.Server"
RUN --mount=type=cache,id=visa2026-nuget,target=/root/.nuget/packages \
    dotnet publish "Visa2026.Blazor.Server.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:NoWarn=DX1000%3BDX1001 \
    /p:InformationalVersion=${APP_VERSION}+${GIT_SHA}

FROM base AS final
WORKDIR /app

# Optional: pass from host so apt can reach mirrors through a corporate proxy, e.g.
#   $env:HTTPS_PROXY='http://127.0.0.1:8888'; .\scripts\local\Build-DockerImages.ps1
ARG HTTP_PROXY
ARG HTTPS_PROXY
ARG NO_PROXY
ENV HTTP_PROXY=$HTTP_PROXY \
    HTTPS_PROXY=$HTTPS_PROXY \
    NO_PROXY=$NO_PROXY

# Enable System.Drawing support for Linux
ENV DOTNET_System_Drawing_EnableUnixSupport=true
# Ensure libgdiplus is found by the .NET P/Invoke layer regardless of arch-specific install path
ENV LD_LIBRARY_PATH=/usr/lib/x86_64-linux-gnu:${LD_LIBRARY_PATH}

# Switch to root user to install dependencies
USER root

# SkiaSharp / GDI+ / font stack. fonts-liberation is always installed; MS Core Fonts are optional (SourceForge download in postinst often blocked by proxy/VPN).
RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
    libfontconfig1 \
    fontconfig \
    libexpat1 \
    libgdiplus \
    libgl1 \
    libglib2.0-0 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    cabextract \
    xfonts-utils \
    fonts-liberation \
    && ln -sf /usr/lib/x86_64-linux-gnu/libgdiplus.so /usr/lib/libgdiplus.so \
    && ( ( echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections \
    && apt-get install -y --no-install-recommends ttf-mscorefonts-installer ) \
    || echo "ttf-mscorefonts-installer skipped (network/SourceForge); using fonts-liberation." ) \
    && fc-cache -f -v \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Switch back to the default app user
USER app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Visa2026.Blazor.Server.dll"]