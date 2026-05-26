---
name: legacy-on-prem-windows-setup
description: >-
  LEGACY: Windows Server + WSL bootstrap only. Scripts in scripts/legacy/on-prem-windows/.
  New on-prem prod uses setup-docker-engine + ON_PREM_LINUX_SERVER.md (Ubuntu). Not SSH or compose.
disable-model-invocation: false
---

# legacy-on-prem-windows-setup — Is this server ready for Docker? (legacy)

> **Deprecated for new on-prem deploys.** Use **[setup-docker-engine](../setup-docker-engine/SKILL.md)** + **[ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md)** (native Ubuntu). This skill is only for existing **Windows Server + WSL** hosts.

**Question this skill answers:** Does this **Windows Server** meet the **hardware and software** requirements in **[ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md)** to run **Docker in WSL 2** and host **Visa2026**?

**Canonical prereq doc (read first):** [docs/ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md)

**Out of scope:** Installing Docker packages, `docker compose up`, SSH — on **Linux**, use [**setup-docker-engine**](../setup-docker-engine/SKILL.md) and [**setup-openssh-server**](../setup-openssh-server/SKILL.md) (Ubuntu). This skill is **legacy Windows Server + WSL** only.

## Quick reference (see doc for full detail)

| Area | Minimum (10 users) |
|------|---------------------|
| RAM / CPU / disk | 8 GB / 2 cores / 100 GB free on `C:` |
| OS | Windows Server 2019/2022 |
| WSL | WSL 2 + **Ubuntu** + **systemd** **running** |
| Docker / compose files | Installed by **setup-docker-engine**; paths under `C:\visa2026` |
| LAN | Inbound **TCP 80**; outbound Docker Hub + MCR + apt |

## Scenarios that hinder Docker Engine setup

Use this list when the user wants Docker but **setup-docker-engine** must stay **blocked** until these are resolved (or explicitly accepted as **WARN**).

**Detect:** `Test-OnPremServerPrerequisites.ps1` (Step 0 / Step 2) · manual checks in [reference.md](./reference.md) · host stories in [learnings.md](./learnings.md).

### A — Blocks handoff to setup-docker-engine (prereq **FAIL**)

Fix in **this skill** before any Docker install.

| # | Scenario | Typical signal | Fix (this skill) |
|---|----------|----------------|------------------|
| A1 | **WSL optional component** not installed | `WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED` in `wsl --status` | `wsl.exe --install --no-distribution` → **reboot** → re-run Step 0 |
| A2 | **`wsl.exe` missing** | Prereq script: `wsl.exe` **FAIL** | Enable WSL feature / Server role; reboot |
| A3 | **No Linux distro** (no Ubuntu) | Prereq: **WSL distro** **FAIL** | `Install-WslDockerEngine.ps1 -SkipDockerInstall` (complete first-login on **RDP/console**) |
| A4 | **systemd not running** in Ubuntu | Prereq: **systemd in WSL** **FAIL**; `systemctl is-system-running` not `running`/`degraded` | `Install-WslDockerEngine.ps1 -SkipDockerInstall` or `-SkipWslInstall -SkipDockerInstall` |
| A5 | **Ubuntu first-time setup** incomplete | Install script hangs; no default user in distro | Finish interactive login on **console/RDP**, not SSH-only |
| A6 | **Reboot skipped** after WSL feature install | Component still missing after `wsl --install` | `Restart-Computer`; re-run Step 0 |

### B — Hinders Docker soon if ignored (prereq **WARN**, fix before compose)

Often discovered **after** Docker install when containers die — still fix in **this skill** when possible.

| # | Scenario | Typical signal | Fix (this skill) |
|---|----------|----------------|------------------|
| B1 | **WSL distro Stopped** | `wsl -l -v` → **Stopped**; later: empty `docker ps`, browser refused | Step **1c**: `C:\Users\<wsl-user>\.wslconfig` → `[wsl2]` `vmIdleTimeout=-1`; `wsl --shutdown` once; start Ubuntu |
| B2 | **WSL 1** instead of WSL 2 | `wsl -l -v` without **VERSION 2** | Convert distro to WSL 2 (`wsl --set-version Ubuntu 2`) |
| B3 | **Low RAM** (&lt; 8 GB visible) | Prereq **RAM** **WARN** | Plan upgrade; light use may work but SQL + app + WSL struggle |
| B4 | **Low disk** on `C:` (&lt; 100 GB free) | Prereq **Disk** **WARN** | Free space or larger volume before image pull + SQL growth |
| B5 | **Not Administrator** | Prereq **Administrator PowerShell** **WARN** | Re-open elevated PowerShell for install scripts |
| B6 | **Weak CPU** (&lt; 2 logical) | Prereq **CPU** **WARN** | Prefer 4+ cores for SQL + first schema update |

### C — Process mistakes (no prereq line; still block or waste time)

| # | Scenario | What goes wrong | Fix |
|---|----------|-----------------|-----|
| C1 | **setup-docker-engine started too early** | Docker/apt on host without Ubuntu/systemd | **STOP** Docker install; complete Step 2 here first |
| C2 | **Full** `Install-WslDockerEngine.ps1` during server prep | 30+ min apt wait; wrong skill phase | Use **`-SkipDockerInstall`** only in this skill |
| C3 | **`-RequireDocker`** during WSL-only phase | Prereq **FAIL** on Docker though WSL OK | Run script **without** `-RequireDocker` until handoff |
| C4 | **Wrong user** for `.wslconfig` | WSL still stops under the account that runs `wsl` | Set `vmIdleTimeout=-1` under **`$env:USERPROFILE`** of that user (`whoami`) |
| C5 | **`wsl --shutdown`** during normal ops | All containers **Exited** | Avoid except after `.wslconfig` change; then `docker compose up -d` in **setup-docker-engine** |

### D — Hinders Docker Engine **install** (audit in Step 0; fix in setup-docker-engine)

Call out early so IT/network is not a surprise when **setup-docker-engine** runs.

| # | Scenario | Typical signal | Where to fix |
|---|----------|----------------|--------------|
| D1 | **Outbound firewall** blocks apt / Docker | `apt` / `curl` timeouts in WSL; pull fails | IT allow list: `archive.ubuntu.com`, `download.docker.com`, `registry-1.docker.io`, `mcr.microsoft.com` — [ON_PREM_PREREQUISITES §3](../../../docs/ON_PREM_PREREQUISITES.md#3-network-requirements) |
| D2 | **Air-gapped** server | No route to Docker/apt mirrors | Offline `.deb` bundle — **setup-docker-engine** + [reference-docker-offline-install.md](../../../scripts/legacy/on-prem-windows/reference-docker-offline-install.md) |
| D3 | **Virtualization disabled** | WSL 2 fails to start or very slow | Enable Hyper-V / VM platform in BIOS/firmware + Windows features |
| D4 | **Docker Desktop** on server | Wrong stack; not supported here | Uninstall Desktop; use Engine **inside WSL** only |
| D5 | **PowerShell wrapper “hangs”** | No output during install | Use direct `wsl ... bash` (documented in **setup-docker-engine** learnings) |

### E — Hinders **compose** after Docker (not Docker Engine itself)

Copy files and secrets **before** `compose up` (**setup-docker-engine**). Mention in Step 0 so deploy planning is complete.

| # | Scenario | Typical signal | Where to fix |
|---|----------|----------------|--------------|
| E1 | Missing **`C:\visa2026\docker-compose.prod.yml`** | Prereq **WARN** / **FAIL** with `-RequireDeployFiles` | Copy from repo; RDP/SSH |
| E2 | Missing **`C:\visa2026\.env.prod`** | Same | Copy from `.env.prod.example`; set `SA_PASSWORD`, `DEVEXPRESS_LICENSEKEY` |
| E3 | Invalid or placeholder **secrets** | SQL or app container crash loop | Edit `.env.prod` on server |

### F — Does **not** block Docker Engine (do not chase in this skill)

| Item | Notes |
|------|--------|
| **sshd** off / port 22 closed | **WARN** only; RDP is enough for server prep |
| **Docker not installed** | **WARN** until **setup-docker-engine** — expected at Step 2 handoff |
| **Docker already installed** | **PASS** — still complete Step 2 gate if WSL/systemd were just fixed |

**Handoff rule:** only when **A** is clear (prereq **FAIL=0** on WSL + systemd) and **B1/B2** addressed for production hosts → tell the user they may use **`@.cursor/skills/setup-docker-engine/`**.

---

## Script allowlist (strict)

| Script | Use |
|--------|-----|
| [Test-OnPremServerPrerequisites.ps1](../../../scripts/legacy/on-prem-windows/Test-OnPremServerPrerequisites.ps1) | Read-only check — run after **each** phase |
| [Install-WslDockerEngine.ps1](../../../scripts/legacy/on-prem-windows/Install-WslDockerEngine.ps1) | WSL + Ubuntu + systemd only: **`-SkipDockerInstall`** |

Manifest: [scripts/legacy/on-prem-windows/README.md](../../../scripts/legacy/on-prem-windows/README.md)

### Forbidden

- `Start-Visa2026Compose.ps1`, `Set-OnPremForceXafDbUpdate.ps1`, `Install-WslDockerEngine-Offline.ps1` — **setup-docker-engine**
- `Install-WindowsOpenSshServer.ps1`, `Repair-WindowsOpenSshServer.ps1` — **setup-openssh-server**
- `scripts/local/**`, `droplet-scripts/**`

### Allowed (not repo scripts)

- `wsl.exe`, `Restart-Computer`, `notepad` for `C:\Users\<user>\.wslconfig`

## Goal (three steps)

| Step | Outcome |
|------|---------|
| **0** | Report: does **hardware/OS** meet Visa2026? |
| **1** | **WSL 2 + Ubuntu + systemd** installed and verified |
| **2** | Re-check: **no FAIL** for host/WSL (Docker may still be **WARN** — OK) → hand off to **setup-docker-engine** |

**Docs:** [ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md) · [ON_PREM_WINDOWS_SERVER.md](../../../docs/legacy/ON_PREM_WINDOWS_SERVER.md) · **Commands:** [reference.md](./reference.md) · **Experience:** [learnings.md](./learnings.md) · **Maturity loop:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md)

### Chat openers

- `@.cursor/skills/legacy-on-prem-windows-setup/` — does this server meet prereqs for Docker / Visa2026?
- Check **RAM, disk, WSL** before installing Docker on Windows Server.
- Server **`10.100.128.25`** — prerequisite audit.
- What could **block Docker Engine** on this Windows Server? (see scenarios section)

### Approval mode

One command per message when the user wants **OK** between steps.

---

## Step 0 — Prerequisite audit (read-only)

**Where:** Server, **Administrator** PowerShell; `C:\visa2026-deploy\` with scripts copied.

```powershell
cd C:\visa2026-deploy
.\Test-OnPremServerPrerequisites.ps1
```

**From admin PC** (optional LAN test to port 22):

```powershell
.\Test-OnPremServerPrerequisites.ps1 -ServerIp 10.100.128.25
```

### How to read results

| Status | Meaning |
|--------|---------|
| **PASS** | Meets requirement |
| **WARN** | Proceed if acceptable (e.g. 8 GB RAM, sshd off, Docker not installed yet, WSL **Stopped**) |
| **FAIL** | Fix before WSL bootstrap (OS/WSL component/no distro/systemd) |

**Exit code 0** = no **FAIL** lines.

### Step 0 checklist

Use the checklist in [ON_PREM_PREREQUISITES.md §6–§8](../../../docs/ON_PREM_PREREQUISITES.md#6-automated-prerequisite-check).

**Not required in Step 0:** Docker installed, `.env.prod`, sshd (WARN is OK).

---

## Step 1 — Install WSL 2 + Ubuntu + systemd

Only if Step 0 showed **FAIL** on WSL component, WSL distro, or systemd.

### 1a — WSL component (first time)

```powershell
wsl.exe --install --no-distribution
Restart-Computer -Force
```

After reboot, re-run Step 0 — **WSL component** should **PASS**.

### 1b — Ubuntu + systemd (no Docker yet)

```powershell
cd C:\visa2026-deploy
.\Install-WslDockerEngine.ps1 -SkipDockerInstall
```

Complete Ubuntu username/password on first prompt (use **RDP/console** if interactive login is required).

If Ubuntu already exists but systemd missing:

```powershell
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipDockerInstall
```

### 1c — Keep WSL from stopping (production)

For the Windows user that runs `wsl` (check `whoami` / `$env:USERPROFILE`):

```powershell
"[wsl2]`nvmIdleTimeout=-1" | Set-Content -Path C:\Users\adm43419\.wslconfig -Encoding ascii
wsl --shutdown
wsl -d Ubuntu -u root -- echo WSL_OK
```

Adjust path if not `adm43419`.

### Step 1 verify

```powershell
wsl -l -v
wsl -d Ubuntu -u root -- systemctl is-system-running
```

Expect: Ubuntu **VERSION 2**, state **Running**, systemd **running**.

---

## Step 2 — Final check → setup-docker-engine

```powershell
.\Test-OnPremServerPrerequisites.ps1
```

**Ready to unlock setup-docker-engine when (all required):**

- [ ] **FAIL=0** on host + WSL + **systemd**
- [ ] **Docker in WSL** is **WARN** or not installed (expected — **do not** install Docker in this skill)
- [ ] `wsl -l -v` shows Ubuntu **Running**, **VERSION 2**
- [ ] User may proceed to **`@.cursor/skills/setup-docker-engine/`** — Docker install is **forbidden** until this point

**Do not run** `Install-WslDockerEngine.ps1` **without** `-SkipDockerInstall` from setup-docker-engine. Do not run `Start-Visa2026Compose.ps1` in this skill.

**Final gate before compose** (run in **setup-docker-engine** phase only):

```powershell
.\Test-OnPremServerPrerequisites.ps1 -RequireDocker -RequireDeployFiles
```

---

## Skill chain (strict order)

```text
legacy-on-prem-windows-setup  →  setup-docker-engine  →  http://<server-ip>
   (MUST complete first)          (BLOCKED until above)
         optional: setup-openssh-server
```

**setup-docker-engine must not start** on this host until this skill’s **Step 2** is done and **FAIL=0** on `Test-OnPremServerPrerequisites.ps1` (Docker **WARN** is OK).

Tell the user explicitly when handing off: *“Windows server setup is complete — you may now use `@.cursor/skills/setup-docker-engine/`.”*

---

## Agent workflow

1. **Read** [learnings.md](./learnings.md) (recent entries); map symptoms to [scenarios](#scenarios-that-hinder-docker-engine-setup) — [maturity loop](../on-prem-deploy/MATURITY.md).
2. Run **only** allowlisted scripts.
3. **Step 0** → interpret PASS/WARN/FAIL; use scenario tables **A–F**.
4. **Step 1** → WSL/Ubuntu/systemd if needed (`-SkipDockerInstall`).
5. **Step 2** → re-run prereq script; confirm handoff to **setup-docker-engine**.
6. Do **not** run `Start-Visa2026Compose.ps1` from this skill.
7. **After** verified success on a host: **append** [learnings.md](./learnings.md) if this chat found a new lesson (try/test/fix).

---

## Investigation map (quick)

Full scenario list: **[Scenarios that hinder Docker Engine setup](#scenarios-that-hinder-docker-engine-setup)** above.

| Prereq line | See scenario |
|-------------|----------------|
| WSL component **FAIL** | A1, A6 |
| WSL distro **FAIL** | A3, A5 |
| systemd **FAIL** | A4 |
| WSL **Stopped** **WARN** | B1 |
| WSL version **WARN** | B2 |
| RAM / disk / CPU / Admin **WARN** | B3–B6 |
| Docker **WARN** | F — **setup-docker-engine** after Step 2 |
| sshd **WARN** | F — optional [setup-openssh-server](../setup-openssh-server/SKILL.md) |

---

## Continuous improvement — skill maturity

This skill **gets more deterministic as it is used**. Shared loop: [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md).

1. **Before** work on a host: **read** [learnings.md](./learnings.md) — skim **## Entries** for this server IP, WSL, systemd, `.wslconfig`.
2. **During** try/test/fix: follow **scenarios A–F** and allowlist only; one command per message when user wants approval.
3. **After** a **verified** fix (prereq **FAIL=0**, Ubuntu **Running**, handoff OK): **append** one dated entry to [learnings.md](./learnings.md) (template in MATURITY.md). Do not append guesses.
4. **Promote** when the same root cause hits **2+** hosts: add a row to [scenarios](#scenarios-that-hinder-docker-engine-setup) or [Investigation map](#investigation-map-quick); at **3+**, add a checklist bullet here or in [reference.md](./reference.md).

**Append only** — never delete or rewrite past entries.
