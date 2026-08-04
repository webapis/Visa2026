#!/usr/bin/env bash
# Publish pre-built manual media + site to on-prem paths (rsync).
# MkDocs build runs on the Windows/build agent; this only copies artifacts.
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: publish-manual-release.sh [options]

  --media-src PATH   Source media tree (screenshots/, videos/)
  --site-src  PATH   Built MkDocs site (index.html)
  --media-dst PATH   Target MANUAL_MEDIA_ROOT
  --site-dst  PATH   Target MANUAL_SITE_ROOT
  --clean-site       Remove site destination before rsync

Example:
  ./scripts/linux/publish-manual-release.sh \
    --media-src ./deploy/manual/media \
    --site-src ./user-manual/site \
    --media-dst /opt/visa2026/manual/media \
    --site-dst /opt/visa2026/manual/site
EOF
}

media_src=""
site_src=""
media_dst=""
site_dst=""
clean_site=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --media-src) media_src="$2"; shift 2 ;;
    --site-src) site_src="$2"; shift 2 ;;
    --media-dst) media_dst="$2"; shift 2 ;;
    --site-dst) site_dst="$2"; shift 2 ;;
    --clean-site) clean_site=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1" >&2; usage; exit 1 ;;
  esac
done

for v in media_src site_src media_dst site_dst; do
  if [[ -z "${!v}" ]]; then
    echo "Missing --${v//_/-}" >&2
    usage
    exit 1
  fi
done

if [[ ! -f "$site_src/index.html" ]]; then
  echo "Built site not found at $site_src (run Build-UserManual.ps1 first)" >&2
  exit 1
fi

mkdir -p "$media_dst" "$site_dst"

echo "Publishing media: $media_src -> $media_dst"
rsync -a --delete "$media_src/" "$media_dst/"

if [[ "$clean_site" -eq 1 ]]; then
  echo "Cleaning site destination: $site_dst"
  rm -rf "${site_dst:?}/"*
fi

echo "Publishing site: $site_src -> $site_dst"
rsync -a "$site_src/" "$site_dst/"

echo "Done. Recreate manual nginx if compose paths changed:"
echo "  docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d manual"
