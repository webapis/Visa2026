#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
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

RUN dotnet restore "Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj"

COPY . .

WORKDIR "/src/Visa2026.Blazor.Server"
RUN dotnet build "Visa2026.Blazor.Server.csproj" -c Release -o /app/build /p:NoWarn=DX1000%3BDX1001

FROM build AS publish
RUN dotnet publish "Visa2026.Blazor.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:NoWarn=DX1000%3BDX1001

FROM base AS final
WORKDIR /app

# Enable System.Drawing support for Linux
ENV DOTNET_System_Drawing_EnableUnixSupport=true
# Ensure libgdiplus is found by the .NET P/Invoke layer regardless of arch-specific install path
ENV LD_LIBRARY_PATH=/usr/lib/x86_64-linux-gnu:${LD_LIBRARY_PATH}

# Switch to root user to install dependencies
USER root

# Force apt to use HTTPS sources; include contrib for ttf-mscorefonts-installer
RUN echo "deb https://deb.debian.org/debian bookworm main contrib" > /etc/apt/sources.list && \
    echo "deb https://deb.debian.org/debian-security/ bookworm-security main contrib" >> /etc/apt/sources.list && \
    echo "deb https://deb.debian.org/debian bookworm-updates main contrib" >> /etc/apt/sources.list

# Install SkiaSharp + GDI+ + font rendering dependencies + Microsoft core fonts (Times New Roman, Arial, etc.)
RUN apt-get update && apt-get install -y \
    libfontconfig1 \
    libexpat1 \
    libgdiplus \
    libgl1 \
    libglib2.0-0 \
    libx11-6 \
    libxext6 \
    libxrender1 \
    cabextract \
    xfonts-utils \
    && ln -sf /usr/lib/x86_64-linux-gnu/libgdiplus.so /usr/lib/libgdiplus.so \
    && echo "ttf-mscorefonts-installer msttcorefonts/accepted-mscorefonts-eula select true" | debconf-set-selections \
    && apt-get install -y ttf-mscorefonts-installer \
    && fc-cache -f -v \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Switch back to the default app user
USER app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Visa2026.Blazor.Server.dll"]