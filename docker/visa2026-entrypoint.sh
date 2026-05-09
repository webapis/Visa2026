#!/usr/bin/env bash
set -euo pipefail

KEYS_DIR="/home/app/.aspnet/DataProtection-Keys"

mkdir -p "${KEYS_DIR}"

# Named volumes are typically owned by root on first mount; fix perms once so the
# app user can write/read keys across container recreates.
chown -R app:app "${KEYS_DIR}" || true
chmod -R u+rwX,g+rwX "${KEYS_DIR}" || true

# Forward CLI args (e.g. docker compose run app -- --updateDatabase --forceUpdate --silent).
# Without this, one-off updater runs start the full web host and never exit.
quoted=()
for arg in "$@"; do
  quoted+=("$(printf '%q' "$arg")")
done
joined="${quoted[*]}"

exec su -s /bin/bash app -c "cd /app && exec dotnet Visa2026.Blazor.Server.dll${joined:+ ${joined}}"

