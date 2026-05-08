#!/bin/bash
# Visa2026 SQL backup (runs on the Droplet)
# Creates a .bak inside the SQL Server container volume and prints the backup path.

set -euo pipefail

TARGET_ENV="${1:-prod}"
case "$TARGET_ENV" in
  prod)
    PROJECT_NAME="visa2026-prod"
    COMPOSE_FILE="docker-compose.prod.yml"
    ENV_FILE=".env.prod"
    ;;
  dev)
    PROJECT_NAME="visa2026-dev"
    COMPOSE_FILE="docker-compose.dev.yml"
    ENV_FILE=".env.dev"
    ;;
  *)
    echo "ERROR: Invalid environment '$TARGET_ENV'. Use 'prod' or 'dev'."
    exit 1
    ;;
esac

if [ ! -f "$COMPOSE_FILE" ]; then
  echo "ERROR: Missing compose file '$COMPOSE_FILE' in $(pwd)"
  exit 1
fi

if [ ! -f "$ENV_FILE" ]; then
  echo "ERROR: Missing env file '$ENV_FILE' in $(pwd)"
  exit 1
fi

set -a
# shellcheck disable=SC1090
. "./$ENV_FILE" >/dev/null 2>&1 || true
set +a

DB_NAME="${DB_NAME:-}"
SA_PASSWORD="${SA_PASSWORD:-}"

if [ -z "$DB_NAME" ]; then
  echo "ERROR: DB_NAME is not set in $ENV_FILE"
  exit 1
fi
if [ -z "$SA_PASSWORD" ]; then
  echo "ERROR: SA_PASSWORD is not set in $ENV_FILE"
  exit 1
fi

SQL_CONTAINER="$(docker ps --format '{{.Names}}' | grep "^${PROJECT_NAME}-sqlserver-" | head -n 1 || true)"
if [ -z "$SQL_CONTAINER" ]; then
  echo "ERROR: Could not find a running SQL container for '${PROJECT_NAME}' (expected '${PROJECT_NAME}-sqlserver-*')."
  echo "Hint: docker compose -p ${PROJECT_NAME} --env-file ${ENV_FILE} -f ${COMPOSE_FILE} ps"
  exit 1
fi

TS="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_DIR="/var/opt/mssql/backup"
BACKUP_FILE="${DB_NAME}_${TS}.bak"
BACKUP_PATH="${BACKUP_DIR}/${BACKUP_FILE}"

echo "Environment: $TARGET_ENV"
echo "Project: $PROJECT_NAME"
echo "SQL container: $SQL_CONTAINER"
echo "Database: $DB_NAME"
echo "Backup path (in container): $BACKUP_PATH"

echo "1. Ensuring backup directory exists..."
docker exec "$SQL_CONTAINER" bash -lc "mkdir -p '$BACKUP_DIR'"

echo "2. Running SQL BACKUP DATABASE..."
docker exec "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -C \
  -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$BACKUP_PATH' WITH INIT, COMPRESSION, CHECKSUM, STATS=10;"

echo "3. Verifying backup file exists and is non-trivial..."
SIZE_BYTES="$(docker exec "$SQL_CONTAINER" bash -lc "stat -c '%s' '$BACKUP_PATH'")"
if [ -z "$SIZE_BYTES" ] || [ "$SIZE_BYTES" -lt 1048576 ]; then
  echo "ERROR: Backup file looks too small (${SIZE_BYTES:-unknown} bytes): $BACKUP_PATH"
  exit 1
fi

echo ""
echo "OK: backup created."
echo "$BACKUP_PATH"

