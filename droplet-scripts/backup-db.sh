#!/bin/bash
# Backward-compatible wrapper. Use droplet-scripts/prod-deploy/backup-db.sh instead.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "${SCRIPT_DIR}/prod-deploy/backup-db.sh" "$@"

