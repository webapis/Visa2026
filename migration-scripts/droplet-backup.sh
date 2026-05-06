#!/usr/bin/env bash
# Run on the Docker host (e.g. SSH session on the droplet).
# Backs up one database from a SQL Server container to a .bak on this host.
#
# Usage:
#   export SA_PASSWORD='...'   # from .env.prod / .env.dev — do not commit
#   ./droplet-backup.sh <container_name> <database_name> [host_output_file]
#
# Example:
#   ./droplet-backup.sh visa2026-prod-sqlserver-1 Visa2026DbProd ./visa2026-prod.bak

set -euo pipefail

CONTAINER="${1:?Usage: SA_PASSWORD=... $0 <container_name> <database_name> [host_output_file]}"
DATABASE="${2:?}"
HOST_OUT="${3:-./visa2026-backup.bak}"

if [[ -z "${SA_PASSWORD:-}" ]]; then
  echo "ERROR: Set SA_PASSWORD in the environment (e.g. from your .env file)." >&2
  exit 1
fi

SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -P "${SA_PASSWORD}")
CONTAINER_BAK="/var/opt/mssql/visa2026-backup-temp.bak"

echo "Backing up [${DATABASE}] in container ${CONTAINER}..."
docker exec "${CONTAINER}" "${SQLCMD[@]}" \
  -Q "BACKUP DATABASE [${DATABASE}] TO DISK = N'${CONTAINER_BAK}' WITH INIT, FORMAT"

echo "Copying to host: ${HOST_OUT}"
docker cp "${CONTAINER}:${CONTAINER_BAK}" "${HOST_OUT}"
echo "Done: ${HOST_OUT}"
