FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install DevExpress license so build tools don't emit DX1000/DX1001 warnings
RUN mkdir -p /root/.config/DevExpress
COPY DevExpress.Key/DevExpress_License.txt /root/.config/DevExpress/DevExpress_License.txt

COPY ["Visa2026.DataImporter/Visa2026.DataImporter.csproj", "Visa2026.DataImporter/"]
RUN dotnet restore "Visa2026.DataImporter/Visa2026.DataImporter.csproj"
COPY . .
WORKDIR "/src/Visa2026.DataImporter"
RUN dotnet publish "Visa2026.DataImporter.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:NoWarn=DX1000%3BDX1001

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Visa2026.DataImporter.dll"]
