#!/usr/bin/env bash
set -euo pipefail

KEYS_DIR="/home/app/.aspnet/DataProtection-Keys"

mkdir -p "${KEYS_DIR}"

# Named volumes are typically owned by root on first mount; fix perms once so the
# app user can write/read keys across container recreates.
chown -R app:app "${KEYS_DIR}" || true
chmod -R u+rwX,g+rwX "${KEYS_DIR}" || true

# Office File API (Resminamalar Word/Excel → PDF) reads the Linux user license
# file, not XAF. Build-stage /root/.config is invisible to the `app` user.
DX_LICENSE_DIR="/home/app/.config/DevExpress"
DX_LICENSE_FILE="${DX_LICENSE_DIR}/DevExpress_License.txt"
mkdir -p "${DX_LICENSE_DIR}"
if [ -s /app/DevExpress_License.txt ]; then
  cp /app/DevExpress_License.txt "${DX_LICENSE_FILE}"
elif [ -n "${DEVEXPRESS_LICENSEKEY:-}" ]; then
  printf '%s' "${DEVEXPRESS_LICENSEKEY}" > "${DX_LICENSE_FILE}"
fi
chown -R app:app /home/app/.config || true
export DevExpress_LicensePath="${DX_LICENSE_FILE}"

# LibreOffice headless (Resminamalar Preview fallback) needs its program dir on LD_LIBRARY_PATH.
LO_PROGRAM="/usr/lib/libreoffice/program"
if [ -d "${LO_PROGRAM}" ]; then
  export LD_LIBRARY_PATH="${LO_PROGRAM}${LD_LIBRARY_PATH:+:${LD_LIBRARY_PATH}}"
fi

# Forward CLI args (e.g. docker compose run app -- --updateDatabase --forceUpdate --silent).
# Without this, one-off updater runs start the full web host and never exit.
quoted=()
for arg in "$@"; do
  quoted+=("$(printf '%q' "$arg")")
done
joined="${quoted[*]}"

exec su -s /bin/bash app -c "export DevExpress_LicensePath='${DX_LICENSE_FILE}'; export LD_LIBRARY_PATH='${LD_LIBRARY_PATH:-}'; cd /app && exec dotnet Visa2026.Blazor.Server.dll${joined:+ ${joined}}"

