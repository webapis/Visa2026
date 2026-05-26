#!/bin/bash
set -euo pipefail
cd /mnt/c/visa2026

# Load secrets without breaking passwords that contain @ or spaces
SA_PASSWORD="$(grep -E '^SA_PASSWORD=' .env.prod | sed 's/\r$//' | cut -d= -f2-)"
DB_NAME="$(grep -E '^DB_NAME=' .env.prod | sed 's/\r$//' | cut -d= -f2-)"
DB_NAME="${DB_NAME:-Visa2026DbProd}"

if [ -z "$SA_PASSWORD" ]; then
  echo "ERROR: SA_PASSWORD missing in .env.prod" >&2
  exit 1
fi

COMPOSE_FILES="-f docker-compose.prod.yml -f docker-compose.restart.override.yml"
COMPOSE=(docker compose -p visa2026-prod --env-file .env.prod $COMPOSE_FILES)

echo "==> Start SQL only"
"${COMPOSE[@]}" up -d sqlserver

echo "==> Wait for SQL Server"
for i in $(seq 1 60); do
  if docker exec visa2026-prod-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
      -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
    echo "SQL is ready"
    break
  fi
  sleep 3
done

echo "==> Ensure database [$DB_NAME] exists"
docker exec visa2026-prod-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -C \
  -Q "IF DB_ID(N'${DB_NAME}') IS NULL CREATE DATABASE [${DB_NAME}]"

echo "==> Verify database"
for i in $(seq 1 30); do
  if docker exec visa2026-prod-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
      -S localhost -U sa -P "$SA_PASSWORD" -C -d "$DB_NAME" -Q "SELECT 1" >/dev/null 2>&1; then
    echo "Database $DB_NAME is online"
    break
  fi
  sleep 2
done

if ! docker exec visa2026-prod-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -d "$DB_NAME" -Q "SELECT 1" >/dev/null 2>&1; then
  echo "ERROR: Database $DB_NAME is not accessible" >&2
  docker exec visa2026-prod-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT name FROM sys.databases"
  exit 1
fi

echo "==> Start app (FORCE_XAF_DB_UPDATE should be true in .env.prod for first deploy)"
"${COMPOSE[@]}" up -d app

"${COMPOSE[@]}" ps
