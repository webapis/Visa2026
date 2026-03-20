# Database Migration Guide for Docker

This document outlines the process for migrating the application's database from one server to another in a Docker environment.

## Scenario

You have been running the Visa2026 application on `Server1`. The SQL Server container has accumulated important data inside a Docker volume (`visa2026_sqlserver_data`). You now need to move the entire application and its data to a new host, `Server2`.

## Recommended Method: Database Backup and Restore

This is the safest and most reliable method. It uses standard SQL Server tools to create a portable backup file, which is independent of the Docker host's environment.

### Step 1: Create Backup on the Source Server (Server1)

1.  **Identify the SQL Server container name:**
    ```sh
    docker compose ps
    ```
    *(Look for a name like `visa2026-sqlserver-1`)*

2.  **Execute the backup command inside the container.** Replace `your_password` with the `SA_PASSWORD` from your `.env` file.
    ```sh
    docker exec -it visa2026-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "your_password" -Q "BACKUP DATABASE [Visa2026Db] TO DISK = N'/var/opt/mssql/backup.bak'"
    ```

3.  **Copy the backup file from the container to the host machine.**
    ```sh
    docker cp visa2026-sqlserver-1:/var/opt/mssql/backup.bak ./backup.bak
    ```
    You now have a `backup.bak` file on `Server1`.

### Step 2: Transfer the Backup File

Use a secure file transfer tool like `scp` or `rsync` to move the `backup.bak` file from `Server1` to `Server2`.

```sh
# Example using scp
scp ./backup.bak your_user@your_server2_ip:~/
```

### Step 3: Restore Backup on the Destination Server (Server2)

1.  **Deploy the application on Server2.** Ensure your `docker-compose.yml` and `.env` files are present. Run the following command, which will create a new, empty database.
    ```sh
    docker compose up -d
    ```

2.  **Copy the backup file into the new SQL Server container.**
    ```sh
    docker cp ./backup.bak visa2026-sqlserver-1:/var/opt/mssql/backup.bak
    ```

3.  **Execute the restore command inside the container.** This will overwrite the empty database with your data.
    ```sh
    docker exec -it visa2026-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "your_password" -Q "RESTORE DATABASE [Visa2026Db] FROM DISK = N'/var/opt/mssql/backup.bak' WITH REPLACE"
    ```

### Step 4: Start the Application and Finalize Migration

The application is now configured to update the database automatically on startup. When you start the `app` service, it will connect to the restored database, detect the version mismatch, and apply all necessary schema and data updates from `Updater.cs`.

The migration is now complete. Your application on `Server2` is running with all the data from `Server1`.

---

## Alternative Method: Docker Volume Migration (Advanced)

This method involves copying the raw volume data directly. It can be faster but is riskier, as it's sensitive to differences in Docker versions and file permissions. **Use with caution.**

1.  **On Server1**: Stop the application and create a tarball of the volume's contents.
    ```sh
    docker compose down
    docker run --rm --volumes-from visa2026-sqlserver-1 -v $(pwd):/backup ubuntu tar cvf /backup/sql-volume-backup.tar /var/opt/mssql
    ```

2.  **Transfer `sql-volume-backup.tar`** from `Server1` to `Server2`.

3.  **On Server2**: Create a new container, restore the volume data from the tarball, and then start the application.
    ```sh
    # Create a temporary container to hold the volume
    docker run -v visa2026_sqlserver_data:/var/opt/mssql --name temp-sql-container ubuntu /bin/bash
    # Restore the data
    docker run --rm --volumes-from temp-sql-container -v $(pwd):/backup ubuntu tar xvf /backup/sql-volume-backup.tar
    # Clean up the temporary container
    docker rm temp-sql-container
    # Start your application
    docker compose up -d
    ```

## In-Place Application Update (No Migration)

If you are updating the application version on an existing server (e.g., deploying a new release), the process is now fully automated.

Simply pull the latest image and restart the services. The application will handle the database schema update on its first run.

```sh
docker compose pull
docker compose up -d
```