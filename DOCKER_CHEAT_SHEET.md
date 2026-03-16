# Docker Command Cheat Sheet for Visa2026

This guide provides a quick reference for the most common Docker and Docker Compose commands used to manage the Visa2026 application.

## 1. Starting the Application

### Standard Start
Use this to start the application and database defined in `docker-compose.yml`.
```bash
docker compose up
```
*   **When to use**: You want to run the app and see the logs in your current terminal window. Press `Ctrl+C` to stop.

### Start in Background (Detached)
```bash
docker compose up -d
```
*   **When to use**: You want the containers to run in the background so you can keep using your terminal.

### Rebuild and Start
```bash
docker compose up --build
```
*   **When to use**: You have changed the code (C#, HTML, etc.) or the `Dockerfile` and need to rebuild the image before starting.

## 2. Stopping and Removing

### Stop and Remove Containers
```bash
docker compose down
```
*   **When to use**: You are finished working and want to stop the application and remove the containers and networks. This **preserves** your database data (stored in the volume).

### Stop, Remove Containers, and Delete Data
```bash
docker compose down -v
```
*   **When to use**: You want a completely fresh start. This **deletes** the SQL Server database volume (`sqlserver_data`). The next time you start, the database will be empty/reset.

### Stop Only (Keep Containers)
```bash
docker compose stop
```
*   **When to use**: You want to pause the containers without removing them.

## 3. Monitoring and Logs

### View Logs
```bash
docker compose logs -f
```
*   **When to use**: You are running in detached mode (`-d`) and need to see the output logs. The `-f` flag follows the logs in real-time.

### View Status
```bash
docker compose ps
```
*   **When to use**: You want to check if the `app` and `sqlserver` containers are running or if they have crashed.

## 4. Maintenance

### Clean Unused Docker Objects
```bash
docker system prune
```
*   **When to use**: Your disk space is low. This deletes stopped containers, unused networks, and build cache.