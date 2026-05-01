#!/bin/bash
# Visa2026 App Update Script (runs on the Droplet)
# Pulls the latest app image and restarts ONLY the app container.
# SQL Server and its data volume are NOT touched.

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

echo "Environment: $TARGET_ENV"
echo "Project: $PROJECT_NAME"
echo "Compose: $COMPOSE_FILE"
echo "Env file: $ENV_FILE"

echo "1. Pulling latest app image from Docker Hub..."
docker compose -p "$PROJECT_NAME" --env-file "$ENV_FILE" -f "$COMPOSE_FILE" pull app

echo "2. Restarting app container (SQL Server keeps running)..."
docker compose -p "$PROJECT_NAME" --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d --no-deps app

echo ""
echo "Done! App updated. Database data is preserved."
docker compose -p "$PROJECT_NAME" --env-file "$ENV_FILE" -f "$COMPOSE_FILE" ps

echo ""
echo "3. Cleaning up dangling image layers..."
docker image prune -f
