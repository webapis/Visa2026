# Officer user manual — on-prem release bundle

How to ship the **manual HTML site** and **screenshot/video media** alongside Visa2026 **without** baking them into the Blazor Docker image.

| Artifact | Source | Deploy target |
|----------|--------|---------------|
| **Media** (PNG/MP4) | `user-manual/assets/` (gitignored; from EasyTest) | `MANUAL_MEDIA_ROOT` → nginx `/manual-media/` |
| **Site** (MkDocs) | `user-manual/site/` after build | `MANUAL_SITE_ROOT` → nginx `/manual/` |
| **App** | `webapia/visa2026` image | unchanged |

Related: [`USER_MANUAL_PIPELINE.md`](USER_MANUAL_PIPELINE.md) · [`USER_MANUAL_E2E_MEDIA.md`](USER_MANUAL_E2E_MEDIA.md) · [`USER_MANUAL_STATUS.md`](USER_MANUAL_STATUS.md)

---

## Playwright E2E (Local + Staging)

Officer journeys use **Microsoft Playwright** for both targets — same tests, different URL:

| Target | Env | Host |
|--------|-----|------|
| **Local E2E** | `VISA2026_E2E_TARGET=Local` (default) | `http://localhost:5050` + fresh `visa2026_easytest` |
| **Staging E2E** | `VISA2026_E2E_TARGET=Staging` | `VISA2026_E2E_BASE_URL` (e.g. `https://10.100.128.25:8080`) |

```powershell
# Local — canonical manual media
.\scripts\local\Record-PlaywrightE2e.ps1 -Target Local

# Staging — full E2E against live staging (manual)
.\scripts\local\Record-PlaywrightE2e.ps1 -Target Staging -BaseUrl 'https://10.100.128.25:8080'
```

Locators: injected `e2e-*` CSS classes on XAF model (`CustomCSSClassName` on BOs / `Model.xafml`).

Filter: `--filter "Driver=Playwright&E2ETarget=Local"` or `...Staging`.

Legacy **EasyTest** tests remain for CI compatibility; new manual media recording uses Playwright.

---

```text
Build agent (Windows, staging)
  Record-EasyTest.ps1  →  user-manual/assets/{screenshots,videos}/
  Publish-ManualRelease.ps1
    → MANUAL_MEDIA_ROOT   (static files)
    → Build-UserManual.ps1 (MANUAL_MEDIA_BASE_URL baked in)
    → MANUAL_SITE_ROOT    (static HTML)

Docker host (/opt/visa2026 or Windows paths)
  manual service (nginx)
    /manual/       ← MANUAL_SITE_ROOT
    /manual-media/ ← MANUAL_MEDIA_ROOT
```

- **Version label** (e.g. `v2026.08`) lives in asset folder names under `screenshots/` and `videos/`.
- Record **once per media version** on a machine that can run EasyTest (`:5050`, Edge, ffmpeg).
- **Promote** the same media + site folders from staging to production (copy/rsync), not regenerate inside the app container.

---

## Docker compose

`docker-compose.prod.yml` and `docker-compose.dev.yml` include a **`manual`** nginx service.

| Variable | Default (repo-relative) | Purpose |
|----------|-------------------------|---------|
| `MANUAL_PORT` | `8082` (prod), `8083` (dev) | Host port for manual nginx |
| `MANUAL_SITE_ROOT` | `./deploy/manual/site` | Built MkDocs output |
| `MANUAL_MEDIA_ROOT` | `./deploy/manual/media` | Mirrors `user-manual/assets/` layout |
| `MANUAL_MEDIA_BASE_URL` | *(build-time only)* | Public URL baked into HTML at MkDocs build |

Example `.env.prod` overrides for Ubuntu (`/opt/visa2026`):

```bash
MANUAL_PORT=8082
MANUAL_SITE_ROOT=/opt/visa2026/manual/site
MANUAL_MEDIA_ROOT=/opt/visa2026/manual/media
```

Set **`MANUAL_MEDIA_BASE_URL`** only when **building** the site (not in compose runtime env):

```bash
export MANUAL_MEDIA_BASE_URL=https://visa2026.example:8082/manual-media
```

Start stack (manual included):

```bash
# App + Postgres only (default)
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d

# Include officer manual nginx (after media/site are published)
COMPOSE_PROFILES=manual docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d
# or: docker compose ... --profile manual up -d manual
```

The **`manual`** service uses compose profile **`manual`** so empty `deploy/manual/` paths do not block a normal app deploy.

Verify:

- `http://<host>:<MANUAL_PORT>/manual/` — officer manual
- `http://<host>:<MANUAL_PORT>/manual-media/screenshots/...` — direct PNG

---

## Release workflow (staging → prod)

### 1. Record media (Windows build agent)

```powershell
.\scripts\local\Record-EasyTest.ps1 -Filter PersonOfficerJourney_LoginCreateEmployeeAddPassport
```

Promotes PNGs/MP4s into `user-manual/assets/` (gitignored).

### 2. Build + publish bundle

```powershell
$base = 'https://10.100.128.25:8082/manual-media'   # must match how officers reach nginx
.\scripts\ci\Publish-ManualRelease.ps1 `
  -ManualMediaBaseUrl $base `
  -MediaTargetRoot 'C:\deploy\manual\media' `
  -SiteTargetDir 'C:\deploy\manual\site' `
  -CleanSite
```

Or step by step:

```powershell
.\scripts\ci\Publish-ManualMedia.ps1 -TargetRoot C:\deploy\manual\media
$env:MANUAL_MEDIA_BASE_URL = 'https://10.100.128.25:8082/manual-media'
.\scripts\ci\Build-UserManual.ps1 -SkipE2E
.\scripts\ci\Publish-UserManualSite.ps1 -TargetDir C:\deploy\manual\site -Clean
```

### 3. Point compose at published paths

Ensure `MANUAL_SITE_ROOT` and `MANUAL_MEDIA_ROOT` in `.env.prod` match the published directories, then recreate nginx if paths changed:

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml --profile manual up -d manual
```

Volume mounts pick up file changes without rebuild; use `-CleanSite` when removing old HTML.

### 4. Promote to production

Copy the **same** `media/` and `site/` trees to the prod host (robocopy, `rsync`, or shared `E:\visa2026-manual-media`). Rebuild the site only if `MANUAL_MEDIA_BASE_URL` differs between staging and prod hostnames.

---

## Linux helper

On Ubuntu (`/opt/visa2026`), after media/site are built on a Windows agent and synced:

```bash
./scripts/linux/publish-manual-release.sh \
  --media-src ./deploy/manual/media \
  --site-src ./user-manual/site \
  --media-dst /opt/visa2026/manual/media \
  --site-dst /opt/visa2026/manual/site
```

MkDocs build still runs on the agent that has Python + repo checkout; this script only **rsyncs** artifacts.

---

## IIS on Windows Server (10.100.128.25)

Recommended for the Calik on-prem host where Visa2026 runs under IIS (not Docker).

### One-time setup (Administrator PowerShell on server)

```powershell
cd C:\visa2026\src\Visa2026
Copy-Item .\scripts\windows-iis\env\manual-release.env.example C:\visa2026\env\manual-release.env
# Edit MANUAL_MEDIA_BASE_URL and paths if needed

.\scripts\windows-iis\Install-Visa2026ManualIisSite.ps1
.\scripts\windows-iis\Enable-Visa2026ManualFirewall.ps1
```

IIS layout:

| URL | Physical path |
|-----|----------------|
| `/manual/` | `C:\visa2026\manual\site` |
| `/manual-media/` | `C:\visa2026\manual\media` |

Officers browse: `https://10.100.128.25:8082/manual/` (enable HTTPS separately if required).

### Publish from build agent on .25

```powershell
cd C:\visa2026\src\Visa2026
git pull

# Prose/media already recorded — rebuild + publish
.\scripts\windows-iis\Publish-Visa2026UserManualRelease.ps1 -CleanSite

# Full release: re-record EasyTest screenshots/videos, then publish
.\scripts\windows-iis\Publish-Visa2026UserManualRelease.ps1 -Record -CleanSite
```

`Publish-Visa2026UserManualRelease.ps1` reads `C:\visa2026\env\manual-release.env` and delegates to `Publish-ManualRelease.ps1`.

**Record prerequisites:** local PostgreSQL, Edge WebDriver, ffmpeg, repo built with **EasyTest** config. EasyTest still targets `http://localhost:5050` (not the staging IIS slot). Recording against a live staging URL is a future enhancement.

### Promote IIS bundle staging → prod

Media and site paths are shared (`C:\visa2026\manual\`). When prod uses a different hostname, rebuild with that host's `MANUAL_MEDIA_BASE_URL` before copying to prod.

---

## IIS (generic)

Same layout: virtual applications or static file roots for `/manual/` and `/manual-media/`. Use the same `Publish-*` scripts with UNC paths (e.g. `\\server\E$\visa2026-manual-media`).

---

## What stays out of git

- `user-manual/assets/**/*.png`, `*.mp4`
- `user-manual/site/`, `user-manual/docs/assets/`
- `deploy/manual/` (local publish staging)

GitHub Pages CI builds prose-only manuals without on-prem media unless a self-hosted runner records assets.

---

## Troubleshooting

| Symptom | Check |
|---------|--------|
| Broken images in manual | `MANUAL_MEDIA_BASE_URL` at build time matches live nginx URL; media tree under `MANUAL_MEDIA_ROOT` mirrors `user-manual/assets/` |
| 404 on `/manual/` | `MANUAL_SITE_ROOT` contains `index.html` from MkDocs; nginx `alias` path ends with `/manual/` |
| Old screenshots after re-record | Re-run `Publish-ManualMedia.ps1`; robocopy uses `/XO` (skip older) — delete stale version folders if needed |
| Reload loop in local preview | Unset `MANUAL_MEDIA_BASE_URL`; use `Serve-UserManual.ps1` |
