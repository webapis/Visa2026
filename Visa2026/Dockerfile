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
