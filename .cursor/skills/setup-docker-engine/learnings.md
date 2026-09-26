# Learnings (append-only): setup-docker-engine

**Purpose:** Record **try → test → fix** on real hosts so Docker install and compose get faster on the next server.

**Maturity loop:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md)

**Target (current):** **Ubuntu on-prem LAN** — [docs/ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md) · `scripts/linux/`

**Legacy entries** below (2026-05-25 …) are **Windows Server + WSL** — keep for history; new entries should tag **linux** or **wsl-legacy**.

**Not here:** DigitalOcean droplet deploy — [visa2026-droplet-prod-deploy](../visa2026-droplet-prod-deploy/SKILL.md).

## Entry template

```markdown
### YYYY-MM-DD — <short title> (<host or context>)

- **Symptom**:
- **Try**:
- **Test**:
- **Fix**:
- **Prevent**:
- **Skill**: setup-docker-engine
```

Promote to [SKILL.md](./SKILL.md) **scenarios** after **2+** hosts.

---

## Entries

### 2026-05-25 — Scenario catalog in SKILL.md

- **Symptom**: Install or compose fails; unclear whether network, WSL, or env.
- **Try**: Map symptom to scenario table before next command.
- **Test**: Gate 0 / hello-world / `compose ps` / HTTP.
- **Fix**: Use **SKILL.md** *Scenarios* (**G0–E**); **G0** → windows-server-setup.
- **Prevent**: Gate 0 before Step 1; hello-world before compose pull.
- **Skill**: setup-docker-engine

---

### 2026-05-25 — Docker install started before WSL/systemd ready

- **Symptom**: `Install-WslDockerEngine.ps1` on host with no Ubuntu or **Stopped** WSL; compose/containers fail later.
- **Try**: Started setup-docker-engine without Gate 0.
- **Test**: Prereq **FAIL** on WSL or systemd.
- **Fix**: Complete **legacy-on-prem-windows-setup** first; Gate 0 **FAIL=0**.
- **Prevent**: Hard dependency in both skills.
- **Skill**: setup-docker-engine

---

### 2026-05-25 — Docker install looked “stuck” in PowerShell wrapper

- **Symptom**: No output after `==> Installing Docker Engine inside WSL`; no `/var/log/visa-docker-install.log`.
- **Try**: `Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig` only.
- **Test**: Direct bash shows apt progress.
- **Fix**: `wsl -d Ubuntu -u root -- bash /mnt/c/WslDocker-Setup/install-docker-engine.sh`
- **Prevent**: Expect **10–30 min** for first `apt`; use direct bash for visibility (**B1**).
- **Skill**: setup-docker-engine

---

### 2026-05-25 — `Start-Visa2026Compose.ps1` stalls after `WSL path:`

- **Symptom**: No further output during pull.
- **Try**: Wait on PS wrapper.
- **Test**: Manual `compose pull` in second window progresses.
- **Fix**: Wait, or `wsl ... bash -lc "cd /mnt/c/visa2026 && docker compose ... up -d"`.
- **Prevent**: First pull (app + SQL 2025) can take **15+ minutes** (**C5**).
- **Skill**: setup-docker-engine

---

### 2026-05-25 — WSL **Stopped** → containers **Exited** (`10.100.128.25`)

- **Symptom**: `docker ps` empty or **Exited**; `wsl -l -v` → Ubuntu **Stopped**; browser **connection refused**.
- **Try**: Hit HTTP without checking WSL.
- **Test**: `wsl -l -v` → **Stopped**.
- **Fix**: `vmIdleTimeout=-1` in `.wslconfig`; `wsl --shutdown` once; `docker compose up -d`; keep Ubuntu **Running**.
- **Prevent**: windows-server-setup Step 1c; do not `wsl --shutdown` during ops (**C4**).
- **Skill**: setup-docker-engine

---

### 2026-05-26 — Skill refocus: Ubuntu on-prem (not WSL)

- **Symptom**: Skill/docs still described WSL + `scripts/legacy/on-prem-windows`.
- **Fix**: [SKILL.md](./SKILL.md) → Docker Engine on **Ubuntu**; [docs/ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md); [scripts/linux/](../../../scripts/linux/).
- **Prevent**: New on-prem prod → Linux path only; Windows runbook deprecated.
- **Skill**: setup-docker-engine

---

### 2026-05-26 — Stability checklist + Linux cutover doc

- **Symptom**: Repeated `ERR_CONNECTION_RESET`; WSL Ubuntu **Stopped**; user asked about Docker Desktop.
- **Try**: Ad-hoc `docker compose up`, Docker Desktop on server.
- **Test**: `Monitor-OnPremWslStack.ps1`; `wsl -l -v` during failures.
- **Fix**: `Repair-OnPremVisa2026Stack.ps1` + `Register-Visa2026WslKeepAliveTask.ps1` (WslPersistent / Startup / KeepAlive). Do **not** use Docker Desktop on Server.
- **Prevent**: [docs/ON_PREM_STABILITY_AND_CUTOVER.md](../../../docs/ON_PREM_STABILITY_AND_CUTOVER.md) §1 checklist; plan Linux VM/droplet per §2 if WSL stays unstable.
- **Skill**: setup-docker-engine

---

### 2026-05-25 — App log `TaskCanceledException` on shutdown

- **Symptom**: `StopHost` + **Session terminated, killing shell** in app logs.
- **Try**: Debug app code first.
- **Test**: `wsl -l -v` when symptom appears.
- **Fix**: Fix WSL **Running** + restart stack.
- **Prevent**: Check WSL before blaming app (**E**).
- **Skill**: setup-docker-engine

---

### 2026-05-25 — Offline Docker (`Install-WslDockerEngine-Offline.ps1`)

- **Symptom**: `Deb folder not found: C:\WslDocker-Setup\debs`
- **Try**: Offline install without preparing debs.
- **Test**: Path missing on server.
- **Fix**: Build `debs` on internet PC per [reference-docker-offline-install.md](../../../scripts/legacy/on-prem-windows/reference-docker-offline-install.md).
- **Prevent**: Offline path only when WSL cannot reach `download.docker.com` (**A3**).
- **Skill**: setup-docker-engine

---

### 2026-08-04 — Docker Desktop on `10.100.128.25` (IIS removed, port remap)

- **Symptom**: Officers stuck on splash; hybrid IIS + Docker Desktop; staging on wrong port `:8081`; WSL Ubuntu **Stopped** (missing `ext4.vhdx`).
- **Try**: `Remove-Visa2026IisDeployment.ps1`; `Repair-OnPremDockerDesktopStack.ps1`; prod `restart app` without postgres.
- **Test**: `http://10.100.128.25/LoginPage` + `:8080/LoginPage` → **200**; IIS sites removed; WSL keepalive tasks disabled.
- **Fix**: IIS slots removed; staging `APP_PORT=8080`; prod **SQL-first** `compose up -d postgres` then `app`; officer notice `C:\visa2026\OFFICER_URLS.txt`.
- **Prevent**: Do not use Docker Desktop on Server long-term — plan Ubuntu + Engine ([ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md)). After Docker restart, never `restart app` alone — postgres first. MemoryMiB in `adm43418` `settings-store.json` may need interactive Docker Desktop quit to apply.
- **Skill**: setup-docker-engine

### 2026-09-23 — Prod pull on `10.100.128.26` (Ubuntu Engine)

- **Symptom**: Officer asked to deploy current repo (`1.0.0.778`) via Docker; host already had `visa2026-prod` up 6 weeks.
- **Try**: `docker compose -p visa2026-prod --env-file .env.prod pull app` then `up -d --no-deps app` from `/opt/visa2026-prod` (not `/opt/visa2026`). Did **not** run `remote-compose-sql-up.sh` (that script still starts SQL Server; this host uses Postgres).
- **Test**: Hub `webapia/visa2026:latest` digest still `sha256:122cab1d...` (image created 2026-08-03). Compose left app Running (same image, no recreate). `http://10.100.128.26/LoginPage` **200**; postgres healthy. This workstation has no Docker / `gh`, so a new image cannot be built or published here.
- **Fix**: Pull/recreate completed; bits unchanged. To ship current HEAD, publish a new Hub tag (CI `workflow_dispatch`) or `docker build` on the server, then pull/recreate.
- **Prevent**: Inventory compose root (`/opt/visa2026-prod`) and image digest before promising a version bump. Do not overwrite Postgres volumes.
- **Skill**: setup-docker-engine

### 2026-09-23 — Hub latest recreate on `10.100.128.26` (Postgres stay-up)

- **Symptom**: New `webapia/visa2026:latest` published to Docker Hub; officers needed the new bits on Ubuntu prod without touching Postgres or `10.100.128.25`.
- **Try**: From `/opt/visa2026-prod`: `docker compose -p visa2026-prod --env-file .env.prod pull app` then `up -d --force-recreate --no-deps app`. Did **not** run `remote-compose-sql-up.sh`. Did **not** set `FORCE_XAF_DB_UPDATE`.
- **Test**: Image `sha256:de6f0e969aad...` Created `2026-09-23T05:14Z` (replaces Aug 3 `122cab1d`). Postgres stayed **healthy**. After recreate, LoginPage failed for ~5+ min while `ApplicationProfileSeedGate` healed (`approval-leg` scanned=4770; `instance organization FKs` filled=12311); app ~80–99% CPU. Then `http://127.0.0.1/LoginPage` and LAN `http://10.100.128.26/LoginPage` → **200**.
- **Fix**: Wait for seed-gate logs after recreate; do not treat high CPU / missing `Now listening` as a hang while heals are still logging. Recreate **app only**.
- **Prevent**: After Hub publish, pull+`--force-recreate --no-deps app`. Expect several minutes of seed heals on a large existing Postgres. Leave `FORCE_XAF` off unless schema drift is confirmed.
- **Skill**: setup-docker-engine
### 2026-09-23 — Hub latest recreate again on `10.100.128.26` (APP_Profile hide ship)

- **Symptom**: New Hub `webapia/visa2026:latest` after person-child DetailView hide; deploy to Ubuntu prod.
- **Try**: SSH `visa2026-onprem-26` / `id_ed25519_visa_onprem_26`. From `/opt/visa2026-prod`: `docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml -f docker-compose.restart.override.yml pull app` then `up -d --force-recreate --no-deps app`. Did **not** run `remote-compose-sql-up.sh`. Plain compose without `-f` fails (`no configuration file provided`).
- **Test**: Image `sha256:f5d7f47c144e` Created `2026-09-23T08:18Z` (replaces morning `642d9ed9`). Postgres stayed **healthy**. Seed gates quick (approval-leg scanned=4826 assigned=0; org FKs filled=0). `http://127.0.0.1/LoginPage` and LAN `http://10.100.128.26/LoginPage` → **200**.
- **Fix**: Always pass both compose `-f` files on this host. Recreate **app only**.
- **Prevent**: Inventory digest before/after pull. Leave Postgres alone. Expect seed heals only if first recreate of the day on large DB.
- **Skill**: setup-docker-engine
### 2026-09-23 — Hub latest recreate on `10.100.128.26` (lookup heal `1.0.0.787`)

- **Symptom**: Deploy Docker Hub `webapia/visa2026:latest` (commit `4cdbc04a`, assembly `1.0.0.787`) to Ubuntu prod.
- **Try**: SSH `visa2026-onprem-26`. From `/opt/visa2026-prod`: `docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml -f docker-compose.restart.override.yml pull app` then `up -d --force-recreate --no-deps app`. Did **not** run `remote-compose-sql-up.sh`. Did **not** set `FORCE_XAF_DB_UPDATE`.
- **Test**: Image `sha256:74280d2c51c4` Created `2026-09-23T11:00:13Z` (replaces `c961361406b7` `09:27Z`). Postgres stayed **healthy** (Up 6 weeks). Seed gates finished quickly (profile updated=36; approval-leg scanned=4826 assigned=0). Host and LAN `http://10.100.128.26/LoginPage` → **200** on the third poll (~45s).
- **Fix**: Pull + `--force-recreate --no-deps app` with both compose files. Recreate **app only**.
- **Prevent**: Confirm digest changed before calling the deploy done. Leave Postgres volume alone.
- **Skill**: setup-docker-engine

### 2026-09-24 — Yellow-marks Azure key on `10.100.128.26` (placeholder)

- **Symptom**: `TEMPLATE_AI_SCAN_AZURE_OPENAI_API_KEY` exists only as a Windows user variable. Prod image is `ASPNETCORE_ENVIRONMENT=Production` (`TemplateAiScan` disabled, `Provider=None`). `docker-compose.prod.yml` does not pass the key into the app.
- **Try**: SSH `visa2026-onprem-26`. Append placeholder `REPLACE_WITH_REAL_AZURE_KEY` to `/opt/visa2026-prod/.env.prod` (mode 600). Add server-only `docker-compose.scan-ai.override.yml` (key + `TemplateAiScan__Enabled` / `ShowInstanceEntry` / `Provider=AzureOpenAI` / endpoint `visa2026-openai` / deployment `gpt-4.1-mini`). Recreate **app only** with all three `-f` files. Did **not** run `remote-compose-sql-up.sh`. Did **not** set `FORCE_XAF_DB_UPDATE`.
- **Test**: Container env `key_len=27`, `provider=AzureOpenAI`, `enabled=true`, `deployment=gpt-4.1-mini`. Postgres stayed **healthy** (Up 6 weeks). Host `http://127.0.0.1/LoginPage` → **200** on poll 4.
- **Fix**: Keep the scan override in the compose `-f` chain. After replacing the placeholder in `.env.prod`, recreate **app only** again or the running process keeps the dummy.
- **Prevent**: Do not put the real key in git or in `docker-compose.prod.yml`. Future pull/recreate must include `docker-compose.scan-ai.override.yml` or the key and Azure flags drop off.
- **Skill**: setup-docker-engine

### 2026-09-24 — Real yellow-marks key recreate on `10.100.128.26`

- **Symptom**: Officer replaced `REPLACE_WITH_REAL_AZURE_KEY` in `/opt/visa2026-prod/.env.prod`. Running app still had the placeholder until recreate.
- **Try**: Same three `-f` files, `up -d --force-recreate --no-deps app`. Did **not** touch Postgres. Did **not** set `FORCE_XAF_DB_UPDATE`.
- **Test**: Container `key_len=84` (placeholder was 27). `provider=AzureOpenAI`, `deployment=gpt-4.1-mini`. Postgres stayed **healthy**. Host `http://127.0.0.1/LoginPage` → **200** on poll 4.
- **Fix**: Recreate **app only** after any `.env.prod` key edit. `docker restart` does not reload the key.
- **Prevent**: Keep `docker-compose.scan-ai.override.yml` in the `-f` chain.
- **Skill**: setup-docker-engine
### 2026-09-24 — Hub latest recreate on `10.100.128.26`

- **Symptom**: Deploy newest Docker Hub `webapia/visa2026:latest` to Ubuntu prod.
- **Try**: SSH `visa2026-onprem-26`. From `/opt/visa2026-prod`: `docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml -f docker-compose.restart.override.yml -f docker-compose.scan-ai.override.yml pull app` then `up -d --force-recreate --no-deps app`. Did **not** run `remote-compose-sql-up.sh`. Did **not** set `FORCE_XAF_DB_UPDATE`.
- **Test**: Image `sha256:bd0e1d7c90db` Created `2026-09-24T07:10:40Z` (replaces `74280d2c51c4` `2026-09-23T10:49Z`). Postgres stayed **healthy** (Up 6 weeks). Seed gates quick (profile updated=36; approval-leg scanned=4829 assigned=0). Host poll 3 and LAN `http://10.100.128.26/LoginPage` → **200**. Scan override still on (`TemplateAiScan__Provider=AzureOpenAI`, `Enabled=true`).
- **Fix**: Pull + `--force-recreate --no-deps app` with all three compose `-f` files. Recreate **app only**.
- **Prevent**: Keep `docker-compose.scan-ai.override.yml` in the `-f` chain or Azure scan flags drop off. Leave Postgres alone.
- **Skill**: setup-docker-engine

### 2026-09-26 — Hub latest recreate on `10.100.128.26` (`1.0.0.801`)

- **Symptom**: Deploy newest Docker Hub `webapia/visa2026:latest` to Ubuntu prod.
- **Try**: SSH `visa2026-onprem-26`. From `/opt/visa2026-prod`: `docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml -f docker-compose.restart.override.yml -f docker-compose.scan-ai.override.yml pull app` then `up -d --force-recreate --no-deps app`. Did **not** run `remote-compose-sql-up.sh`. Did **not** set `FORCE_XAF_DB_UPDATE`.
- **Test**: Image `sha256:e504327a1ca2` Created `2026-09-25T13:21:50Z` (replaces `bd0e1d7c90db` `2026-09-24T07:10Z`). Module assembly `1.0.0.801`. Postgres stayed **healthy** (Up 7 weeks). Seed gates quick (profile updated=36; approval-leg scanned=4829 assigned=0). Host poll 2 and LAN `http://10.100.128.26/LoginPage` → **200**. Scan override still on (`TemplateAiScan__Provider=AzureOpenAI`, `Enabled=true`).
- **Fix**: Pull + `--force-recreate --no-deps app` with all three compose `-f` files. Recreate **app only**.
- **Prevent**: Keep `docker-compose.scan-ai.override.yml` in the `-f` chain. Leave Postgres alone.
- **Skill**: setup-docker-engine

### 2026-09-26 — Hub latest recreate on `10.100.128.26` staging (`1.0.0.801`)

- **Symptom**: Deploy the same Docker Hub `webapia/visa2026:latest` to the staging stack on the Ubuntu host. Staging app was not running; postgres had been up 7 weeks.
- **Try**: SSH `visa2026-onprem-26`. From `/opt/visa2026-staging`: `docker compose -p visa2026-staging --env-file .env.prod -f docker-compose.prod.yml -f docker-compose.restart.override.yml pull app` then `up -d --force-recreate --no-deps app`. Two compose files only (no scan override on this stack). Did **not** run `remote-compose-sql-up.sh`. Did **not** set `FORCE_XAF_DB_UPDATE`.
- **Test**: Image `sha256:e504327a1ca2` (same as prod). Postgres stayed **healthy** on `127.0.0.1:5433`. Seed heals were heavy (profile created=36; approval-leg scanned=4770 names=4770; org FKs filled=12311) so LoginPage stayed closed for several minutes. Then `http://127.0.0.1:8080/LoginPage` → **200**.
- **Fix**: Pull + `--force-recreate --no-deps app`. Recreate **app only**. Wait out seed heals before treating connection refused as a failed deploy.
- **Prevent**: Staging compose root is `/opt/visa2026-staging`, project `visa2026-staging`, `APP_PORT=8080`. Do not add the prod scan override here.
- **Skill**: setup-docker-engine