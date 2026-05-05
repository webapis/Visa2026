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
docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Image}}"
```

## Logs

```powershell
docker logs visa2026-local-app-1 --tail 200
```

## DB update one-off (same stack as running `app`)

From **repository root**:

```powershell
cd C:\Users\webap\Documents\GitHub\Visa2026

docker compose -p visa2026-local --env-file .env.local -f docker-compose.prod.yml run --rm --no-deps app --updateDatabase --forceUpdate
```

Non-interactive (if supported by your build):

```powershell
docker compose -p visa2026-local --env-file .env.local -f docker-compose.prod.yml run --rm --no-deps app --updateDatabase --forceUpdate --silent
```

If Compose does not append args correctly to the image entrypoint:

```powershell
docker compose -p visa2026-local --env-file .env.local -f docker-compose.prod.yml run --rm --no-deps --entrypoint dotnet app Visa2026.Blazor.Server.dll --updateDatabase --forceUpdate --silent
```

## Recreate app after DB update

```powershell
docker compose -p visa2026-local --env-file .env.local -f docker-compose.prod.yml up -d --force-recreate --no-deps app
```

## Pull + recreate (new image already tagged in `.env`)

```powershell
docker compose -p visa2026-local --env-file .env.local -f docker-compose.prod.yml pull app
docker compose -p visa2026-local --env-file .env.local -f docker-compose.prod.yml up -d --force-recreate --no-deps app
```

## Droplet (SSH, example prod)

```bash
cd ~/visa2026
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull app
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d --no-deps app
```

## Local image build (same as CI args)

```powershell
cd C:\Users\webap\Documents\GitHub\Visa2026
.\scripts\local\Build-DockerImages.ps1
# Optional: .\scripts\local\Build-DockerImages.ps1 -DeployLocal
```

Requires `DevExpress.Key\DevExpress_License.txt`.
