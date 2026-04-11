FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Visa2026.DataImporter/Visa2026.DataImporter.csproj", "Visa2026.DataImporter/"]
RUN dotnet restore "Visa2026.DataImporter/Visa2026.DataImporter.csproj"
COPY . .
WORKDIR "/src/Visa2026.DataImporter"
RUN dotnet publish "Visa2026.DataImporter.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Visa2026.DataImporter.dll"]
