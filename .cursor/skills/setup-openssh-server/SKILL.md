---
name: setup-openssh-server
description: >-
  Install and repair OpenSSH Server (sshd) on company Windows Server for remote admin.
  Covers domain-joined hosts (DOMAIN\user login, GSSAPI resets). Accumulates host experience
  in learnings.md (read before, append after verified fixes). Uses scripts/on-prem/.
  Optional before Docker; not WSL or compose. Never droplet-scripts or scripts/local.
disable-model-invocation: false
---

# setup-openssh-server — OpenSSH on Windows Server

**Scope:** **OpenSSH Server** (`sshd`) on a **Windows Server** host so admins can run PowerShell remotely (copy scripts, run on-prem deploy commands). Works on **workgroup** and **domain-joined** servers.

**Not this skill:** WSL, Docker, Visa2026 compose — [visa2026-windows-server-setup](../visa2026-windows-server-setup/SKILL.md), [setup-docker-engine](../setup-docker-engine/SKILL.md).

**Runbook:** [ON_PREM_WINDOWS_SERVER.md](../../../docs/ON_PREM_WINDOWS_SERVER.md) · **Network:** [ON_PREM_PREREQUISITES §3](../../../docs/ON_PREM_PREREQUISITES.md#3-network-requirements) (TCP **22** optional inbound).

## Quick reference

| Item | Default |
|------|---------|
| Service | `sshd` (Automatic) |
| Port | **22** / TCP |
| Config | `C:\ProgramData\ssh\sshd_config` |
| Scripts on server | `C:\visa2026-deploy\` |
| Domain login | `DOMAIN\username` (script prints detected domain) |

## Skill position (optional, any order)

```text
visa2026-windows-server-setup  →  setup-docker-engine
setup-openssh-server           →  optional anytime (often before remote-only work)
```

SSH is **not** required for Visa2026 users (HTTP **80**). RDP/console is enough to run other skills if you do not need remote shell.

---

## Domain-joined servers

| Topic | Guidance |
|-------|----------|
| **Login format** | From client: `ssh DOMAIN\adm43419@10.100.128.25` — repair script prints `DOMAIN\user` when it detects AD |
| **vs local account** | `.\localuser@server` or `server\localuser` for non-domain accounts |
| **Connection reset after host key** | Common on **admin** or **domain** accounts — run **Step 2** `Repair-WindowsOpenSshServer.ps1` with **`-TestUser`** = short name (e.g. `adm43419`), not `DOMAIN\adm43419` |
| **Repair script** | Sets `GSSAPIAuthentication no`, `StrictModes no` (domain-friendly on OpenSSH 8.x); fixes `administrators_authorized_keys` ACLs |
| **Isolate domain vs sshd** | Repair tests `Administrator@127.0.0.1` — if that reaches auth but domain user resets, involve **IT** (below) |
| **IT / GPO (domain user only)** | **Allow log on locally** (or appropriate right) on **this server**; account not locked/expired; profile `C:\Users\<user>` OK |

**Workgroup server:** same install/repair flow; use local `ServerName\user` or `.\user`.

---

## Scenarios that hinder OpenSSH setup

| # | Scenario | Signal | Fix (this skill) |
|---|----------|--------|------------------|
| S1 | **Not Administrator** | Install script fails / no firewall rule | Elevated **Administrator** PowerShell on server |
| S2 | **Capability without sshd.exe** | OpenSSH.Server “Installed” but no service | `Install-WindowsOpenSshServer.ps1` (Win32 zip path) |
| S3 | **No outbound GitHub** | Download zip fails | Pre-copy `OpenSSH-Win64.zip`; `Install-... -ZipPath C:\Temp\OpenSSH-Win64.zip` |
| S4 | **Firewall blocks 22** | `sshd` running, LAN cannot connect | Re-run install (creates rule) or manual inbound **TCP 22**; check corporate firewall |
| S5 | **Port 22 in use** | Install warns: not listening | Find conflicting listener; change `-Port` on install + client |
| S6 | **Connection reset** (admin/domain) | Drops right after host key | `Repair-WindowsOpenSshServer.ps1 -TestUser <shortname>` |
| S7 | **Missing sshd-session.exe** (OpenSSH 9+) | Repair suggests reinstall | `Repair-... -ReinstallWin32OpenSsh` or `Install-... -ForceWin32BinaryInstall` |
| S8 | **Bad sshd_config** | `sshd -t` fails | Repair restores keys; use `.bak-*` under `C:\ProgramData\ssh\` |
| S9 | **Domain user: permission denied** | Admin test OK; domain fails | IT: logon rights, AD lockout, password expiry |
| S10 | **Wrong client username** | Auth fails | Use `DOMAIN\user` from repair output, not only `user` |
| S11 | **Pubkey-only mismatch** | Password never prompted | Repair enables `PasswordAuthentication yes`; or use pubkey with correct ACLs |
| S12 | **Match Group administrators** | Admin account reset | Repair shows Match blocks; quarantine/fix `administrators_authorized_keys` |

---

## Script allowlist (strict)

| Script | Step |
|--------|------|
| [Install-WindowsOpenSshServer.ps1](../../../scripts/on-prem/Install-WindowsOpenSshServer.ps1) | 1 — install + start `sshd` |
| [Repair-WindowsOpenSshServer.ps1](../../../scripts/on-prem/Repair-WindowsOpenSshServer.ps1) | 2 — reset / domain / ACL / config |
| [Test-OnPremServerPrerequisites.ps1](../../../scripts/on-prem/Test-OnPremServerPrerequisites.ps1) | Verify only — `sshd` / TCP 22 (optional `-ServerIp` from admin PC) |

Manifest: [scripts/on-prem/README.md](../../../scripts/on-prem/README.md)

### Forbidden

- `Install-WslDockerEngine.ps1`, `Start-Visa2026Compose.ps1`, and all other on-prem deploy scripts
- `scripts/local/**`, `droplet-scripts/**`

### Allowed (not repo scripts)

- `ssh`, `scp`, `Test-NetConnection` on admin workstation
- `notepad` for `sshd_config` (prefer repair script first)

---

## Goal (two steps + verify)

| Step | Outcome |
|------|---------|
| **1** | `sshd` **Running**, **Automatic**, TCP **22** listening |
| **2** | Successful `ssh DOMAIN\user@<server-ip>` from admin PC (or repair completes with handshake OK) |

**Commands:** [reference.md](./reference.md) · **Experience:** [learnings.md](./learnings.md) · **Maturity loop:** [on-prem-windows-deploy/MATURITY.md](../on-prem-windows-deploy/MATURITY.md)

### Chat openers

- `@.cursor/skills/setup-openssh-server/` — enable SSH on Windows Server `10.100.128.25`
- SSH **connection reset** on domain admin account
- Server is **domain joined** — how do I log in?

### Approval mode

One command per message when the user wants **OK** between steps.

---

## Step 0 — Preconditions

**On server (RDP/console if SSH not up yet):**

- [ ] **Administrator** PowerShell
- [ ] Scripts in `C:\visa2026-deploy\` (copy from repo)
- [ ] Optional offline: [OpenSSH-Win64.zip](https://github.com/PowerShell/Win32-OpenSSH/releases/latest/download/OpenSSH-Win64.zip) on USB

**From admin PC (after Step 1):**

```powershell
Test-NetConnection -ComputerName 10.100.128.25 -Port 22
```

---

## Step 1 — Install OpenSSH Server

**Where:** Server, **Administrator** PowerShell.

```powershell
cd C:\visa2026-deploy
.\Install-WindowsOpenSshServer.ps1
```

**Offline:**

```powershell
.\Install-WindowsOpenSshServer.ps1 -ZipPath C:\Temp\OpenSSH-Win64.zip
```

**Verify on server:**

```powershell
Get-Service sshd
Get-NetTCPConnection -LocalPort 22 -State Listen -ErrorAction SilentlyContinue
```

**Verify from admin PC:**

```powershell
cd C:\visa2026-deploy
.\Test-OnPremServerPrerequisites.ps1 -ServerIp 10.100.128.25
```

Expect: **sshd service** **PASS**, **PC -> server TCP 22** **PASS** (when run from PC with LAN access).

### Step 1 checklist

- [ ] `sshd` **Running**, StartType **Automatic**
- [ ] Port **22** in **Listen** state
- [ ] Inbound firewall rule for OpenSSH (unless `-SkipFirewall` was used intentionally)

---

## Step 2 — Repair (connection reset / domain login)

Run when: connection **resets** after accepting host key, **domain** admin cannot log in, or repair script reported missing binaries.

```powershell
cd C:\visa2026-deploy
.\Repair-WindowsOpenSshServer.ps1 -TestUser adm43419
```

Use the **short** account name (`adm43419`), not `CALIKSOA\adm43419`. Replace with the account you will use for SSH.

**Heavy repair:**

```powershell
.\Repair-WindowsOpenSshServer.ps1 -ReinstallWin32OpenSsh -TestUser adm43419 -ZipPath C:\Temp\OpenSSH-Win64.zip
```

**Test from admin PC:**

```powershell
ssh -o PreferredAuthentications=password DOMAIN\adm43419@10.100.128.25
```

Use the domain name printed by the repair script.

### Step 2 checklist

- [ ] Repair ends without “port 22 not listening”
- [ ] Interactive SSH from admin PC works
- [ ] If domain user still fails but `Administrator@127.0.0.1` reaches auth → escalate **IT** (scenario **S9**)

---

## Agent workflow

1. **Read** [learnings.md](./learnings.md) and [scenarios](#scenarios-that-hinder-openssh-setup) — [maturity loop](../on-prem-windows-deploy/MATURITY.md).
2. Confirm **domain vs workgroup**; note expected `DOMAIN\user` format.
3. **Step 1** — `Install-WindowsOpenSshServer.ps1` (allowlist only).
4. Verify service + port + optional `Test-OnPremServerPrerequisites.ps1 -ServerIp`.
5. On reset/auth failure → **Step 2** `Repair-WindowsOpenSshServer.ps1 -TestUser <shortname>`.
6. Do **not** run WSL/Docker scripts from this skill.
7. One command per message if user wants approval mode.
8. **After** verified SSH login: **append** [learnings.md](./learnings.md) for new domain/reset lessons.

---

## Investigation map (quick)

| Signal | Action |
|--------|--------|
| No `sshd` service | Step 1 install |
| Port 22 closed | S4 — firewall / install |
| Connection reset | Step 2 repair; see **S6–S8** |
| Domain user only fails | **S9** — IT logon rights / AD |
| `Deb`/zip / GitHub errors | **S3** — offline zip |
| Docker/WSL issues | Other skills — not SSH |

---

## Continuous improvement — skill maturity

This skill **gets more deterministic as it is used**. Shared loop: [on-prem-windows-deploy/MATURITY.md](../on-prem-windows-deploy/MATURITY.md).

1. **Before** install/repair: **read** [learnings.md](./learnings.md) — domain reset, offline zip, capability-without-sshd.
2. **During** try/test/fix: use scenario **S1–S12**; `-TestUser` = short name on domain hosts.
3. **After** verified `ssh DOMAIN\user@host` (or documented IT escalation): **append** [learnings.md](./learnings.md).
4. **Promote** after **2+** repeats: add to [scenarios](#scenarios-that-hinder-openssh-setup) or [Investigation map](#investigation-map-quick).

**Append only** — never delete or rewrite past entries.
