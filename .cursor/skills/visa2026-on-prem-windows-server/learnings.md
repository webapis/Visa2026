# Learnings (append-only): On-prem Windows Server (SSH + WSL Docker)

Purpose: capture **try/error** fixes from real LAN server setups (OpenSSH, WSL, Docker in Ubuntu, remote admin from a PC). Agents **read before** helping on a similar host; **append after** a verified fix.

Keep **`SKILL.md`** as the runbook; **promote** into `SKILL.md` only when the same lesson has recurred on **2+ servers**.

**Runbook:** [docs/ON_PREM_WINDOWS_SERVER.md](../../../docs/ON_PREM_WINDOWS_SERVER.md)

## How to use

**Before** SSH port closed, missing `sshd`, WSL “no distro”, Docker install “hang”, or remote `docker compose` failures: skim **## Entries**.

**After** the server is verified (`ssh` from PC, `docker run hello-world` in WSL, compose `ps` OK): append one entry (date, server/host, symptom, root cause, fix, prevent).

```markdown
### YYYY-MM-DD — <host or scenario>

- **Symptom**:
- **Root cause**:
- **Fix**:
- **Prevent**:
```

---

## Entries

### 2026-05-25 — Reference host `10.100.128.25` (SSH + WSL + Docker bootstrap)

- **Symptom**: From admin PC, `Test-NetConnection` to port **22** failed (`TcpTestSucceeded : False`) while **ping** succeeded. On server, `Get-Service *ssh*` showed only **`ssh-agent`**, not **`sshd`**. `Get-WindowsCapability` reported **OpenSSH.Server Installed**, but `C:\Windows\System32\OpenSSH` had only client tools (`ssh.exe`, no `sshd.exe`, no `install-sshd.ps1`). `Remove-WindowsCapability` for repair threw **`Element not found`**.
- **Root cause**: Broken/partial OpenSSH Server deployment via DISM capability (common on Windows Server). Server was never registered with Win32 **sshd** service. Port 22 had no listener.
- **Fix**: Run **`Install-WindowsOpenSshServer.ps1`** — skips DISM remove when `sshd.exe` missing; downloads **Win32-OpenSSH** zip; runs **`install-sshd.ps1`**; starts **`sshd`** Automatic; firewall **22**. Verified: **`OK: Port 22 is listening`**, then `ssh adm43419@10.100.128.25` from PC.
- **Prevent**:
  - Do not assume **Installed** capability means **`sshd` runs** — always `Get-Service sshd` and `Get-NetTCPConnection -LocalPort 22 -State Listen`.
  - Do not rely on `C:\Windows\System32\OpenSSH\install-sshd.ps1` existing; script uses **`C:\OpenSSH-Setup\OpenSSH-Win64`** zip path.
  - Phase order for remote admin: **SSH first**, then `scp` scripts from PC, then WSL/Docker on server.

---

### 2026-05-25 — PowerShell script paste / encoding (`Install-WindowsOpenSshServer.ps1`)

- **Symptom**: Running copied `.ps1` failed with **`Unexpected token 'Using'`**, **`Missing using directive`**, **`string is missing the terminator`** at lines mentioning `Write-Host "Using zip: $zip"`.
- **Root cause**: User pasted script fragments **bottom-to-top** into PowerShell (commands ran in reverse). Separately, **smart quotes** or **Unicode em dashes** from copy/paste can break the parser.
- **Fix**: Paste **one complete script block** top-to-bottom once. Repo scripts use **ASCII-only** strings and concatenation (`'Using zip: ' + $zip`). Re-copy file from git, do not retype in Notepad.
- **Prevent**: In skill/chat, give **single fenced blocks** labeled “run once”. Prefer `powershell.exe -ExecutionPolicy Bypass -File .\Script.ps1`.

---

### 2026-05-25 — `Set-ExecutionPolicy -Bypass` on Windows Server PowerShell 5.1

- **Symptom**: `Set-ExecutionPolicy -Scope Process -Bypass -Force` → **`A parameter cannot be found that matches parameter name 'Bypass'`**.
- **Root cause**: Older **Windows PowerShell 5.1** uses `-ExecutionPolicy Bypass`, not `-Bypass`.
- **Fix**: `Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force` (included at top of on-prem scripts).
- **Prevent**: On-prem scripts target **5.1** + `#Requires -RunAsAdministrator`; test on Server PS, not only PowerShell 7.

---

### 2026-05-25 — WSL not enabled (`wsl --status` / `wsl -l -v`)

- **Symptom**: `wsl --status` and `wsl -l -v` reported **`WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED`** and suggested `wsl.exe --install --no-distribution`.
- **Root cause**: WSL optional component never installed; only attempted Ubuntu/`Install-WslDockerEngine` with **`-SkipWslInstall`**.
- **Fix**: `wsl.exe --install --no-distribution` → message **reboot required** → reboot → `wsl --status` OK → `wsl --install Ubuntu` (or `.\Install-WslDockerEngine.ps1` without skip).
- **Prevent**:
  - Never **`-SkipWslInstall`** when `wsl -l -v` lists **no distros**.
  - `Install-WslDockerEngine.ps1` detects optional component and stops with explicit reboot instructions.

---

### 2026-05-25 — `Install-WslDockerEngine.ps1 -SkipWslInstall` with empty distros

- **Symptom**: **`Distro not found: Ubuntu. Available:`** (empty list).
- **Root cause**: **`-SkipWslInstall`** used before any Linux distribution registered.
- **Fix**: Run **`.\Install-WslDockerEngine.ps1`** without **`-SkipWslInstall`** after WSL component reboot; complete Ubuntu first-login (user/password).
- **Prevent**: Gate: `wsl -l -v` must show **Ubuntu** (or pass **`-DistroName`** matching listed name, e.g. `Ubuntu-22.04`).

---

### 2026-05-25 — Parsing `wsl -l -q` in Windows PowerShell

- **Symptom**: `Get-WslDistroNames` returned empty though Ubuntu existed.
- **Root cause**: **`wsl -l -q`** output can include **UTF-16/BOM** spacing quirks in **Windows PowerShell 5.1**.
- **Fix**: Parse **`wsl -l -v`** lines with regex for `Running|Stopped` instead; fallback to **`-l -q`** trim BOM/nulls.
- **Prevent**: Scripts use **`wsl -l -v`** first; auto-pick first `Ubuntu*` distro via **`Resolve-WslDistroName`**.

---

### 2026-05-25 — Docker install looked “stuck” at `Installing Docker Engine inside WSL`

- **Symptom**: After `==> Installing Docker Engine inside WSL (Ubuntu)`, no further output for many minutes.
- **Root cause**: **`Invoke-WslExe`** captured all WSL stdout until bash script finished (long `apt-get`); user thought hang.
- **Fix**: Script updated: **`-LiveOutput`** streams `==> apt-get update (1/4)` lines; write **`install-docker-engine.sh`** with Unix LF. Secondary check: `wsl -d Ubuntu -u root -- pgrep apt`.
- **Prevent**: Tell operator **10–30 min** wait; use **`-SkipSystemdConfig`** only when `systemctl is-system-running` already **`running`**.

---

### 2026-05-25 — Ubuntu first install on Server (`wsl --install Ubuntu`)

- **Symptom**: Download 84%… then **Create a default Unix user account**, **opt-in metrics** prompt.
- **Root cause**: Normal first provisioning of WSL Ubuntu on Server.
- **Fix**: Set Linux user (e.g. **`adm43419`**) and password; answer metrics **Y/n**; `exit` to return to PowerShell. Verify `wsl -l -v` → **Ubuntu**, **VERSION 2**.
- **Prevent**: Document that **RDP/console** may be needed for first Ubuntu login if SSH session cannot answer interactively.

---

### 2026-05-25 — systemd required for Docker service in WSL

- **Symptom**: Docker install/start unreliable without systemd.
- **Root cause**: Default WSL instance did not boot **systemd**.
- **Fix**: `/etc/wsl.conf` with `[boot] systemd=true`, then **`wsl --shutdown`**, verify `wsl -d Ubuntu -u root -- systemctl is-system-running` → **`running`**.
- **Prevent**: **`Enable-WslSystemd`** in `Install-WslDockerEngine.ps1`; skip only with **`-SkipSystemdConfig`** when already verified.

---

### 2026-05-25 — Remote Docker management model (SSH from PC)

- **Symptom**: Operator expected to SSH “into Docker” directly on Windows.
- **Root cause**: Docker Engine runs **inside WSL Linux**, not as native Windows Docker Desktop.
- **Fix**: Workflow: **PC → `ssh user@server` → PowerShell → `wsl -d Ubuntu -u root -- docker …`**. Compose files on **`C:\visa2026`** → `/mnt/c/visa2026` in WSL.
- **Prevent**: Skill and [reference.md](./reference.md) **§ Remote Docker from your PC**; do not point to Docker Desktop on server.

---

### 2026-05-25 — Confusing `wsl` output when commands chained with `>>`

- **Symptom**: `wsl -l -v` then `wsl --status` pasted together showed stale **“no installed distributions”** mixed with success output.
- **Root cause**: **Incomplete paste** or reading output from an **earlier** run before Ubuntu finished installing.
- **Fix**: Run **`wsl -l -v`** alone after Ubuntu install; trust latest output only.
- **Prevent**: One command per line when diagnosing; record timestamp/host in [ON_PREM_WINDOWS_SERVER.md](../../../docs/ON_PREM_WINDOWS_SERVER.md) status table.
