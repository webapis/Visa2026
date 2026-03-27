# DigitalOcean Droplet Deployment Guide

This document provides a step-by-step guide to deploying the Visa2026 application and its SQL Server database to a DigitalOcean Droplet using Docker.

## 1. Prerequisites

*   A DigitalOcean account.
*   The `webapia/visa2026` image published to Docker Hub.
*   SSH access to your local machine.

## 2. Step 1: Create the Droplet

1.  Log in to DigitalOcean and click **"Create"** -> **"Droplets"**.
2.  **Choose an Image**: Go to the **"Marketplace"** tab and search for **"Docker"**. Select the official Docker 1-Click image (Ubuntu-based).
3.  **Choose a Plan**: Select a plan with at least **2GB RAM** (e.g., $12/month). SQL Server requires significant memory to run stably.
4.  **Authentication**: Choose **SSH Key** (recommended) or a strong password.
5.  **Finalize**: Click **"Create Droplet"**. Note down the **IP Address** of your new Droplet (e.g., `64.226.112.29`).

## 3. Step 2: Prepare Deployment Files

On the Droplet, you will need a `docker-compose.yml` and a `.env` file.

### docker-compose.yml
```yaml
services:
  app:
    image: webapia/visa2026:latest
    ports:
      - "80:8080"  # Maps Droplet port 80 to App port 8080
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=Visa2026Db;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True
    depends_on:
      - sqlserver

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest # or 2025-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SA_PASSWORD}
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

volumes:
  sqlserver_data:
```

### .env
```env
SA_PASSWORD=YourStrongPassword!
```

## 4. Step 3: Deployment via SSH

1.  **Connect to the Droplet**:
    ```sh
    ssh root@YOUR_DROPLET_IP
    ```

2.  **Clean up Default Services (If using 1-Click Docker)**:
    The DigitalOcean Docker image often comes with Traefik running on port 80. Remove it to avoid port conflicts:
    ```sh
    docker stop traefik && docker rm traefik
    ```

3.  **Setup Application Directory**:
    ```sh
    mkdir visa2026 && cd visa2026
    ```

4.  **Create Configuration Files**:
    Use `nano` or `vim` to create the files on the server:
    ```sh
    nano .env
    # Paste your .env content, then Ctrl+O, Enter, Ctrl+X

    nano docker-compose.yml
    # Paste your docker-compose content, then Ctrl+O, Enter, Ctrl+X
    ```

5.  **Start the Containers**:
    ```sh
    docker compose up -d --remove-orphans
    ```

## 5. Step 4: Firewall Configuration

DigitalOcean Droplets have the `UFW` firewall enabled by default. You must open the ports you wish to access:

```sh
ufw allow 80/tcp      # For web access
ufw allow 1433/tcp    # (Optional) For remote database access
```

## 6. Monitoring and Troubleshooting

### Check Container Status
```sh
docker compose ps
```

### View Application Logs
If the app is not working, check the logs for errors (e.g., database connection issues):
```sh
docker compose logs -f app
```

### Common Issue: "Server is in script upgrade mode"
When a SQL Server container starts for the first time, it may block connections for 30-60 seconds. If your app crashes with a "Login failed" error initially, simply restart the app container once the database is ready:
```sh
docker compose start app
```

### Access the Application
Visit `http://YOUR_DROPLET_IP` in your browser.
