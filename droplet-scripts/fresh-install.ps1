# Visa2026 Fresh Install Script
# This script wipes all local Docker footprints and restarts from scratch.

Write-Host "1. Stopping containers and deleting volumes..." -ForegroundColor Cyan
docker compose down -v

Write-Host "2. Purging Docker system (Images, Cache, and Networks)..." -ForegroundColor Cyan
# -a removes all images, not just dangling ones
# -f forces without confirmation
docker system prune -af --volumes

Write-Host "3. Pulling the latest images from Docker Hub..." -ForegroundColor Cyan
docker compose pull

Write-Host "4. Starting the application and database..." -ForegroundColor Cyan
docker compose up -d

Write-Host ""
Write-Host "Done! The application is performing a fresh start." -ForegroundColor Green
Write-Host "You can check the logs using: docker compose logs -f" -ForegroundColor White
docker compose ps