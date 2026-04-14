#!/bin/bash
# Visa2026 Fresh Install Script for Droplet (Linux)
# This script wipes all local Docker footprints and restarts from scratch.

echo "1. Stopping containers and deleting volumes..."
docker compose down -v

echo "2. Purging Docker system (Images, Cache, and Networks)..."
docker system prune -af --volumes

echo "3. Pulling the latest images from Docker Hub..."
docker compose pull

echo "4. Starting the application and database..."
docker compose up -d

echo -e "\nDone! The application is performing a fresh start."
docker compose ps
