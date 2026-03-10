# Publishing to Docker Hub

This document outlines the automated process for building the Visa2026 application's Docker image and publishing it to Docker Hub.

## 1. Overview

The project uses a **GitHub Actions workflow** to achieve Continuous Integration. When you push changes to the `main` branch of the GitHub repository, a workflow is automatically triggered. This workflow:

1.  Builds a new Docker image based on the `Dockerfile`.
2.  Tags the image with the unique commit SHA and a `latest` tag.
3.  Logs into Docker Hub using credentials stored securely as GitHub Secrets.
4.  Pushes the tagged images to your specified Docker Hub repository.

This ensures that your Docker Hub repository always has an up-to-date, runnable image of your application.

## 2. Prerequisites

*   A free or paid **Docker Hub account**.
*   The project code hosted in a **GitHub repository**.
*   A repository created on Docker Hub (e.g., `yourusername/visa2026`).

## 3. Configuration Steps

Follow these steps to configure the automated publishing process.

### Step 1: Create a Docker Hub Access Token

For security, you should not use your Docker Hub password directly in GitHub. Instead, create a Personal Access Token.

1.  Log in to your Docker Hub account.
2.  Navigate to **Account Settings** by clicking on your username in the top-right corner.
3.  Go to the **Security** tab.
4.  Click **New Access Token**.
5.  Give the token a descriptive name (e.g., `github-actions-visa2026`).
6.  Set the permissions to **Read, Write, Delete**.
7.  Click **Generate**.
8.  **Important**: Copy the generated token immediately. You will not be able to see it again.

### Step 2: Configure GitHub Secrets

Next, store your Docker Hub credentials securely in your GitHub repository.

1.  In your GitHub repository, go to **Settings** > **Secrets and variables** > **Actions**.
2.  Click **New repository secret** for each of the secrets below:
    *   **`DOCKERHUB_USERNAME`**: Your Docker Hub username.
    *   **`DOCKERHUB_TOKEN`**: The access token you generated in the previous step.

### Step 3: Configure the GitHub Actions Workflow

The workflow file needs to know the name of your Docker Hub repository.

1.  Open the workflow file located at `.github/workflows/publish-to-docker-hub.yml`.
2.  Find the `env` section in the file.
3.  Update the `DOCKER_IMAGE_NAME` variable to match your Docker Hub username and repository name.

    ```yaml
    env:
      # Configuration: Set this to your Docker Hub repository
      DOCKER_IMAGE_NAME: yourdockerhubusername/visa2026 # <-- TODO: Replace with "your-username/your-repo-name"
    ```

    For example, if your username is `johndoe` and your repository is `visa-app`, you would change it to:

    ```yaml
    DOCKER_IMAGE_NAME: johndoe/visa-app
    ```

4.  Commit and push this change to your `main` branch.

## 4. How It Works

Once you have completed the configuration:

1.  **Commit and Push**: Make any change to your application code and push it to the `main` branch.
2.  **Workflow Trigger**: The push event will automatically trigger the "Build and Publish to Docker Hub" workflow.
3.  **Monitor Progress**: You can view the workflow's progress in real-time by going to the **Actions** tab in your GitHub repository.
4.  **Verify on Docker Hub**: After the workflow completes successfully, navigate to your repository on Docker Hub. You will see the new images, one tagged with `latest` and another with the specific commit SHA (e.g., `f2a4b5c...`).

Your application is now published and can be pulled and run on any machine with Docker installed using:

```sh
docker run -p 8080:8080 yourdockerhubusername/visa2026:latest
```

*(Note: The command above does not include the SQL Server database. For a full deployment, you would still use a `docker-compose.yml` file that references your new Docker Hub image instead of building it locally.)*