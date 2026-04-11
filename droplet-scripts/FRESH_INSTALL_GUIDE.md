# Fresh Install Script Guide (Linux/Droplet)

This document explains how to use the `fresh-install.sh` script to perform a "factory reset" of your Docker environment on the DigitalOcean Droplet.

## What this script does
1. **Stops all containers** and deletes the associated networks.
2. **Deletes the SQL Server volume**, wiping all database data.
3. **Purges the Docker system**, removing all cached images and build layers to ensure you aren't using old code.
4. **Pulls the latest images** from Docker Hub.
5. **Restarts the application** in detached mode.

## How to use it

### 1. Transfer the script to your Droplet
If the script is not already on your server, you can create it manually:
```bash
nano ~/visa2026/fresh-install.sh
```
Paste the contents of the script, save (`Ctrl+O`, `Enter`), and exit (`Ctrl+X`).

### 2. Make the script executable
Linux requires explicit permission to run a shell script. Run this command inside your `visa2026` directory:
```bash
chmod +x fresh-install.sh
```

### 3. Execute the script
Run the script using the following command:
```bash
./fresh-install.sh
```

## Troubleshooting

*   **Permission Denied**: Ensure you ran the `chmod +x` command mentioned in step 2.
*   **Missing .env**: The script assumes your `.env` file (containing `SA_PASSWORD`) is in the same directory. If it is missing, SQL Server will fail to start.
*   **Docker Hub Errors**: Ensure your Droplet has internet access to pull the `webapia/visa2026` image.

## Post-Installation
After the script finishes, you can verify the health of your services by running:
```bash
docker compose ps
```