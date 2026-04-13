#!/bin/bash
# Visa2026 App Update Script (runs on the Droplet)
# Pulls the latest app image and restarts ONLY the app container.
# SQL Server and its data volume are NOT touched.

echo "1. Pulling latest app image from Docker Hub..."
docker compose pull app

echo "2. Restarting app container (SQL Server keeps running)..."
docker compose up -d --no-deps app

echo ""
echo "Done! App updated. Database data is preserved."
docker compose ps
