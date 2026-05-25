---
name: setup-docker-engine
description: >-
  Install Docker Engine in WSL and deploy Visa2026 from Docker Hub. HARD DEPENDENCY on
  visa2026-windows-server-setup. Accumulates host experience in learnings.md (read before,
  append after verified fixes). Uses setup-docker-engine allowlist in scripts/on-prem/.
  Not SSH. Never droplet-scripts or scripts/local.
disable-model-invocation: false
---

# setup-docker-engine — Docker in WSL + Visa2026 compose

**Scope:** **Docker Engine** in an existing **WSL 2 Ubuntu** instance, then **`docker compose`** for Visa2026 from **Docker Hub**.

## Hard dependency — do not start Docker until server setup is complete

**This skill MUST NOT run** until **[visa2026-windows-server-setup](../visa2026-windows-server-setup/SKILL.md)** has finished successfully on this host.

| Blocked until windows-server-setup is done | Why |
|--------------------------------------------|-----|
| `Install-WslDockerEngine.ps1` (any flags) for **first-time** WSL/Ubuntu | Use windows-server-setup: `-SkipDockerInstall` only |
| `Install-WslDockerEngine-Offline.ps1` | Needs Ubuntu + systemd first |
| `Start-Visa2026Compose.ps1` | Needs Docker Engine first |
| `wsl --install`, `Restart-Computer` for WSL | windows-server-setup only |

**If the user asks for Docker/compose but WSL is missing:** stop and invoke **`@.cursor/skills/visa2026-windows-server-setup/`** — do not improvise WSL install from this skill.

**Requirements doc:** [ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md)

### windows-server-setup is complete when

- [ ] [visa2026-windows-server-setup](../visa2026-windows-server-setup/SKILL.md) **Step 2** done on this server
- [ ] `Test-OnPremServerPrerequisites.ps1` → **FAIL=0** (Docker may still be **WARN** — that is expected)
- [ ] `wsl -l -v` → Ubuntu **VERSION 2**, state **Running**
- [ ] `wsl -d Ubuntu -u root -- systemctl is-system-running` → **running** (or **degraded**)

**SSH** is optional — [**setup-openssh-server**](../setup-openssh-server/SKILL.md).

## Scenarios that hinder Docker Engine installation (and compose)

Use when **Step 1** (`docker --version`, `hello-world`) or **Step 2** (`compose up`, HTTP) fails. Map symptoms to a row, then apply the fix in the right skill.

**Detect:** Gate 0 / Step 1–2 verify commands · [reference-docker-install.md](./reference-docker-install.md) · [learnings.md](./learnings.md) · host scenarios in [visa2026-windows-server-setup](../visa2026-windows-server-setup/SKILL.md#scenarios-that-hinder-docker-engine-setup).

### G0 — Wrong phase: stop this skill (windows-server-setup first)

| # | Scenario | Typical signal | Fix |
|---|----------|----------------|-----|
| G0.1 | **WSL / Ubuntu / systemd** not ready | Gate 0 **FAIL** on WSL component, distro, or systemd | **visa2026-windows-server-setup** — do not run Step 1 here |
| G0.2 | **Ubuntu Stopped** before install | `wsl -l -v` → **Stopped**; `docker` errors or WSL exits | **visa2026-windows-server-setup** scenario **B1** (`.wslconfig` `vmIdleTimeout=-1`) |
| G0.3 | **WSL 1** distro | No **VERSION 2** in `wsl -l -v` | **visa2026-windows-server-setup** scenario **B2** |
| G0.4 | **Full bootstrap** run from this skill | `Install-WslDockerEngine.ps1` without `-SkipWslInstall -SkipSystemdConfig` | Wrong skill; complete server prep, then retry Step 1 with correct flags |

### A — Blocks successful Docker Engine install (Step 1)

| # | Scenario | Typical signal | Fix (this skill) |
|---|----------|----------------|------------------|
| A1 | **Outbound blocked** to Docker/apt | `apt` / `curl` timeout; `curl https://download.docker.com` not **200** | IT allow list — [ON_PREM_PREREQUISITES §3](../../../docs/ON_PREM_PREREQUISITES.md#3-network-requirements); or **A2** |
| A2 | **Air-gapped** host (no mirrors) | Online install never finishes; no route to registries | [reference-docker-offline-install.md](../../../scripts/on-prem/reference-docker-offline-install.md) + `Install-WslDockerEngine-Offline.ps1` |
| A3 | **Offline bundle missing / wrong** | `Deb folder not found: C:\WslDocker-Setup\debs` | Build `debs` on internet PC; match Ubuntu **codename**; copy to server |
| A4 | **Install script path missing** | `bash: /mnt/c/WslDocker-Setup/install-docker-engine.sh: No such file` | Copy `scripts/on-prem` staging to `C:\WslDocker-Setup\` (run `Install-WslDockerEngine.ps1` once or copy repo files) |
| A5 | **WSL stops mid-apt** | Install aborts; later `wsl -l -v` → **Stopped** | Fix **G0.2** in server prep; restart Step 1 |
| A6 | **systemd / docker service** not up | `systemctl status docker` failed; `Cannot connect to docker daemon` | Confirm Gate 0 systemd **PASS**; `wsl -d Ubuntu -u root -- systemctl start docker` |
| A7 | **Disk full** in WSL or on `C:` | `apt` **No space left on device** | Free `C:` / WSL disk; expand volume before retry |
| A8 | **Corporate HTTP proxy** required | `apt` 407 / connection refused despite “internet” on Windows | Configure `apt` proxy in Ubuntu or use **offline** debs |
| A9 | **Wrong distro name** | Commands target `Ubuntu` but only `Ubuntu-22.04` exists | `-DistroName` on allowlisted scripts (see [reference.md](./reference.md)) |

### B — Install looks failed but may still succeed (Step 1 patience / visibility)

| # | Scenario | Typical signal | What to do |
|---|----------|----------------|------------|
| B1 | **PowerShell wrapper buffers output** | No lines after `Installing Docker Engine inside WSL` for 10+ min | Direct bash: `wsl -d Ubuntu -u root -- bash /mnt/c/WslDocker-Setup/install-docker-engine.sh`; tail `/var/log/visa-docker-install.log` |
| B2 | **First `apt` is slow** | 10–30 min on first server | Wait; do not reboot; check log tail |
| B3 | **hello-world pull slow** | Hang on `Pulling from library/hello-world` | Wait; confirm **A1** outbound to `registry-1.docker.io` |
| B4 | **Gate 0 run with `-RequireDocker`** | **FAIL** on Docker though WSL OK | Re-run Gate 0 **without** `-RequireDocker`; Docker **WARN** is expected before Step 1 |

### C — Docker installed but hinders Step 2 / production (compose + HTTP)

| # | Scenario | Typical signal | Fix (this skill) |
|---|----------|----------------|------------------|
| C1 | **Image pull blocked** | `pull access denied`, timeout pulling `webapia/visa2026` or MCR SQL | IT: `registry-1.docker.io`, `hub.docker.com`, `mcr.microsoft.com`; retry `Start-Visa2026Compose.ps1 -Pull` or manual `compose pull` |
| C2 | **Missing deploy files** | `compose file not found`; prereq **FAIL** with `-RequireDeployFiles` | Copy `docker-compose.prod.yml` + `.env.prod` to `C:\visa2026\` |
| C3 | **Invalid `.env.prod`** | SQL or app **Exited** immediately; logs show auth/license errors | Fix `SA_PASSWORD`, `DEVEXPRESS_LICENSEKEY` in `C:\visa2026\.env.prod` |
| C4 | **WSL Stopped after install** | `docker ps` empty; containers **Exited**; browser refused | **visa2026-windows-server-setup** **B1**; then `compose up -d` again |
| C5 | **`Start-Visa2026Compose.ps1` stalls** | Stops after `WSL path:` during pull | Wait 15+ min or manual `wsl ... compose ... up -d` ([reference.md](./reference.md)) |
| C6 | **Windows firewall** blocks HTTP | Containers **Up** but browser refused from LAN | `Start-Visa2026Compose.ps1 -OpenHttpFirewall` or allow inbound **TCP 80** |
| C7 | **Port 80 already in use** on Windows | Compose bind error on `0.0.0.0:80` | Change `APP_PORT` in `.env.prod` or stop conflicting service |
| C8 | **Wrong image tag** | Pull OK but app crash / missing build | Set `APP_IMAGE_TAG` in `.env.prod` to published tag |
| C9 | **Host RAM pressure** | SQL OOM / slow first start on 8 GB | Plan 16 GB; allow first schema update to finish |

### D — Process mistakes in this skill

| # | Scenario | What goes wrong | Fix |
|---|----------|-----------------|-----|
| D1 | **Skipped Gate 0** | Install on broken WSL | Run `Test-OnPremServerPrerequisites.ps1`; **FAIL=0** on WSL/systemd |
| D2 | **`wsl --shutdown`** during install or compose | All containers die | Avoid; after `.wslconfig` change only; then `compose up -d` |
| D3 | **Docker Desktop** used instead of WSL Engine | Wrong daemon / confusion | Uninstall Desktop; Engine only **inside Ubuntu** |
| D4 | **Compose before hello-world** | Pull failures blamed on app | Finish Step 1 verify first |
| D5 | **Left `FORCE_XAF_DB_UPDATE` enabled** | Repeated heavy migrations | `Set-OnPremForceXafDbUpdate.ps1 -Disable` after healthy start |

### E — Usually not an install failure (do not misdiagnose)

| Item | Notes |
|------|--------|
| **`systemctl` degraded** | **PASS** for Gate 0 if `running` or `degraded` |
| **App `TaskCanceledException` on shutdown** | Often WSL stop SIGTERM — fix **C4**, not app code ([learnings.md](./learnings.md)) |
| **sshd / port 22** | Unrelated to Docker Engine install |
| **15+ min first compose pull** | Normal for app + SQL 2025 images |

**Step 1 success:** `docker --version`, `docker compose version`, **`Hello from Docker!`**.

**Step 2 success:** `compose ps` → **app** + **sqlserver** **Up**; `http://<server-ip>` loads login.

---

## Gate 0 — confirm server setup (mandatory before Step 1)

Run on the server **before** any Docker install command:

```powershell
cd C:\visa2026-deploy
.\Test-OnPremServerPrerequisites.ps1
```

**Proceed to Step 1 only if:**

- Script exits **0** (**FAIL=0**)
- No **FAIL** on: **WSL component**, **WSL distro**, **systemd in WSL**
- **Docker in WSL** may be **WARN** (this skill installs it)

**STOP and switch to [visa2026-windows-server-setup](../visa2026-windows-server-setup/SKILL.md) if:**

- Any **FAIL** on WSL or systemd
- Ubuntu **Stopped** in `wsl -l -v` (fix `.wslconfig` in windows-server-setup)
- `systemctl` not **running**

Do **not** run `Install-WslDockerEngine.ps1` without `-SkipWslInstall -SkipSystemdConfig` from this skill.

## Script allowlist (strict)

Only these under [`scripts/on-prem/`](../../../scripts/on-prem/):

| Script | Step |
|--------|------|
| [Test-OnPremServerPrerequisites.ps1](../../../scripts/on-prem/Test-OnPremServerPrerequisites.ps1) | **0** — Gate: windows-server-setup complete (read-only) |
| [Install-WslDockerEngine.ps1](../../../scripts/on-prem/Install-WslDockerEngine.ps1) | 1 — Docker only (`-SkipWslInstall -SkipSystemdConfig`) |
| [Install-WslDockerEngine-Offline.ps1](../../../scripts/on-prem/Install-WslDockerEngine-Offline.ps1) | 1b — air-gapped `.deb` install |
| [Start-Visa2026Compose.ps1](../../../scripts/on-prem/Start-Visa2026Compose.ps1) | 2 — pull/up prod stack |
| [Set-OnPremForceXafDbUpdate.ps1](../../../scripts/on-prem/Set-OnPremForceXafDbUpdate.ps1) | 2 — one-shot `FORCE_XAF_DB_UPDATE` |

Manifest: [scripts/on-prem/README.md](../../../scripts/on-prem/README.md)

### Forbidden for this skill

- **`Install-WslDockerEngine.ps1` without `-SkipWslInstall -SkipSystemdConfig`** — full bootstrap is **visa2026-windows-server-setup**
- `Install-WindowsOpenSshServer.ps1`, `Repair-WindowsOpenSshServer.ps1` — **setup-openssh-server**
- `scripts/local/**`, `droplet-scripts/**`, other `scripts/on-prem/*.ps1`
- `wsl --install`, `Restart-Computer` for WSL — **visa2026-windows-server-setup**

### Allowed for Gate 0 only (read-only)

- `Test-OnPremServerPrerequisites.ps1` — verify windows-server-setup is complete; **do not** use it to fix WSL (defer to windows-server-setup)

### Allowed without being repo scripts

- `wsl.exe` on the server for verify/logs
- `notepad` for `C:\visa2026\.env.prod`
- Inline `wsl ... docker compose ...` for read-only **ps/logs** when no script fits

## Goal (Gate 0 + two steps)

| Step | What you get |
|------|----------------|
| **0** | Confirm **visa2026-windows-server-setup** complete (`Test-OnPremServerPrerequisites.ps1`, FAIL=0) |
| **1** | **Docker Engine** + Compose plugin in WSL; `hello-world` OK |
| **2** | **visa2026-prod** stack **Up** at `http://<server-ip>:80` |

**Docs:** [ON_PREM_WINDOWS_SERVER.md](../../../docs/ON_PREM_WINDOWS_SERVER.md) · **Commands:** [reference.md](./reference.md) · **Detail:** [reference-docker-install.md](./reference-docker-install.md) · **Experience:** [learnings.md](./learnings.md) · **Maturity loop:** [on-prem-windows-deploy/MATURITY.md](../on-prem-windows-deploy/MATURITY.md)

### Chat openers

- `@.cursor/skills/setup-docker-engine/` — install Docker in WSL, deploy Visa2026 from Docker Hub.
- Containers **Exited**, WSL **Stopped**, browser **connection refused** on on-prem server.
- What scenarios **block Docker Engine install** on this server?

**Not this skill:** first-time server/WSL setup — use **visa2026-windows-server-setup** first.

### Approval mode

One command per message when the user wants **OK** between steps.

---

## Step 1 — Install Docker Engine (WSL)

**Where:** Server, **Administrator** PowerShell; scripts in `C:\visa2026-deploy\`.

```powershell
cd C:\visa2026-deploy
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig
```

If the PowerShell wrapper shows no progress:

```powershell
wsl -d Ubuntu -u root -- bash /mnt/c/WslDocker-Setup/install-docker-engine.sh
```

**Offline / no apt from WSL:** [reference-docker-offline-install.md](../../../scripts/on-prem/reference-docker-offline-install.md) + `Install-WslDockerEngine-Offline.ps1`.

**Verify:**

```powershell
wsl -d Ubuntu -u root -- docker --version
wsl -d Ubuntu -u root -- docker compose version
wsl -d Ubuntu -u root -- docker run --rm hello-world
```

### Step 1 checklist

- [ ] **Hello from Docker!**
- [ ] `wsl -l -v` → Ubuntu **Running** while you work (if **Stopped**, server prep skill: `vmIdleTimeout=-1` in `.wslconfig`)

---

## Step 2 — Deploy Visa2026 (Docker Hub)

**Requires:** `C:\visa2026\docker-compose.prod.yml` and `C:\visa2026\.env.prod` (placed by server prep / RDP / **setup-openssh-server**).

```powershell
cd C:\visa2026-deploy
.\Start-Visa2026Compose.ps1 -Pull -OpenHttpFirewall
```

If the script stalls after `WSL path:`:

```powershell
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d"
```

**Verify:**

```powershell
wsl -l -v
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod ps"
```

Expect: **app** and **sqlserver** **Up**, `0.0.0.0:80->8080/tcp`. Browser: `http://<server-ip>`.

**Schema one-shot:** `Set-OnPremForceXafDbUpdate.ps1 -Enable` then `-Disable` after healthy start.

### Step 2 checklist

- [ ] Both containers **Up** (not **Exited**)
- [ ] HTTP login page loads

---

## Agent workflow

1. **Read** [learnings.md](./learnings.md); map to [scenarios](#scenarios-that-hinder-docker-engine-installation-and-compose) (**G0–E**) — [maturity loop](../on-prem-windows-deploy/MATURITY.md).
2. If **G0** applies → **`@.cursor/skills/visa2026-windows-server-setup/`** first; **do not** install Docker.
3. **Gate 0** → `Test-OnPremServerPrerequisites.ps1`; require **FAIL=0** for WSL/systemd.
4. **Step 1** — Docker allowlist scripts only (`-SkipWslInstall -SkipSystemdConfig` or offline).
5. **Step 2** — `Start-Visa2026Compose.ps1`; verify `ps` + HTTP.
6. One command per message if user wants approval mode.
7. **After** verified hello-world / compose **Up** / HTTP: **append** [learnings.md](./learnings.md) for new try/test/fix lessons.

---

## Investigation map (quick)

Full list: **[Scenarios that hinder Docker Engine installation](#scenarios-that-hinder-docker-engine-installation-and-compose)**.

| Signal | Scenario |
|--------|----------|
| No WSL/Ubuntu / Gate 0 FAIL | **G0** → **visa2026-windows-server-setup** |
| `docker: command not found` | **A1–A9** / Step 1 |
| Install “stuck”, no output | **B1–B2** |
| `Deb folder not found` | **A3** |
| `hello-world` OK, pull fails | **C1** |
| Containers **Exited**, WSL **Stopped** | **G0.2** / **C4** |
| Browser refused, containers Up | **C6–C7** |
| Missing compose/env | **C2–C3** |

Details: [learnings.md](./learnings.md).

---

## Continuous improvement — skill maturity

This skill **gets more deterministic as it is used**. Shared loop: [on-prem-windows-deploy/MATURITY.md](../on-prem-windows-deploy/MATURITY.md).

1. **Before** Docker install or compose on a host: **read** [learnings.md](./learnings.md) — especially WSL **Stopped**, install “hang”, pull stall, **Exited** containers.
2. **During** try/test/fix: cite scenario IDs (**A3**, **B1**, **C4**, …) when known; prefer documented `wsl ... bash` fallbacks over improvising.
3. **After** **Hello from Docker!** and/or stack **Up** + HTTP confirmed: **append** [learnings.md](./learnings.md) with host context (template in MATURITY.md).
4. **Promote** after **2+** repeats: extend [scenarios](#scenarios-that-hinder-docker-engine-installation-and-compose) or [Investigation map](#investigation-map-quick); after **3+**, update Step checklists or [reference.md](./reference.md).

**Append only** — never delete or rewrite past entries.
