# Reference: compose and DB commands (Visa2026)

## Chat opener (step-by-step + your OK)

```text
Coding is done—walk me through local Docker image build and container recreate; propose each step and wait for my OK.
```

Paste that into chat with **`@.cursor/skills/visa2026-lifecycle-docker/`** if you want this skill in context.

---

Paths are **Windows examples**; on the droplet use `~/visa2026`, `visa2026-prod`, `.env.prod`, and bash.

Adjust **`-p`**, **`--env-file`**, and **`-f`** to match `docker compose ls` for your machine.

## Resolve app container name

```powershell
.\scripts\local\lifecycle-docker\Docker-ListContainers.ps1
```

## Logs

```powershell
.\scripts\local\lifecycle-docker\Docker-AppLogs.ps1 -Tail 200
```

## DB update one-off (same stack as running `app`)

```powershell
.\scripts\local\lifecycle-docker\Compose-UpdateDatabase.ps1
```

Non-interactive (if supported by your build):

```powershell
.\scripts\local\lifecycle-docker\Compose-UpdateDatabase.ps1 -Silent
```

## Recreate app after DB update

```powershell
.\scripts\local\lifecycle-docker\Recreate-App.ps1
```

## Recreate app (local by default) / optional pull

```powershell
# Default: use locally built image tag (:local) and recreate app (no registry pull)
.\scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1

# If you want to pull from the registry for the tag in your env file, use -Pull:
.\scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1 -Pull
```

## Droplet deploy

Use `droplet-scripts/update-app.ps1` / `droplet-scripts/update-app.sh` (and the production docs) for server-side pulls/recreates.

## Local image build (same as CI args)

```powershell
.\scripts\local\lifecycle-docker\Build-DockerImages.ps1
# Optional: .\scripts\local\lifecycle-docker\Build-DockerImages.ps1 -DeployLocal
```

Requires `DevExpress.Key\DevExpress_License.txt`.
