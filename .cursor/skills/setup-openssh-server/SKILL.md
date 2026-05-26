---
name: setup-openssh-server
description: >-
  Install and configure OpenSSH Server on company Ubuntu (on-prem LAN) for remote
  admin (bash, scp, deploy scripts). Accumulates experience in learnings.md. Uses
  scripts/linux/. Legacy Windows Server path uses scripts/legacy/on-prem-windows/ PowerShell only.
  Optional before setup-docker-engine. Never droplet-scripts or scripts/local.
disable-model-invocation: false
---

# setup-openssh-server — OpenSSH on Ubuntu (on-prem)

**Scope:** **`openssh-server`** on **Ubuntu 22.04/24.04** so admins can SSH in for deploy, logs, and `scripts/linux/` maintenance. Standard Linux `sshd` — not Windows Win32-OpenSSH.

**Not this skill:** Docker / compose — [setup-docker-engine](../setup-docker-engine/SKILL.md).

**Runbook:** [ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md) · **Prereqs:** [ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md) (TCP **22** optional inbound)

## Quick reference (Ubuntu)

| Item | Default |
|------|---------|
| Package | `openssh-server` |
| Service | `ssh` (`systemctl`) |
| Port | **22** / TCP |
| Config | `/etc/ssh/sshd_config` |
| Deploy path on server | `/opt/visa2026/` |
| Login | `ssh user@<server-ip>` (Linux user, not `DOMAIN\user`) |

## Skill position (optional)

```text
setup-openssh-server  →  optional (remote admin from admin PC)
setup-docker-engine   →  Docker Engine + Visa2026 compose
```

SSH is **not** required for Visa2026 users (HTTP **80**). Console access is enough if you do not need remote shell.

## Legacy: Windows Server

Win32 OpenSSH (`Install-WindowsOpenSshServer.ps1`, domain `DOMAIN\user`, connection reset repair) is **deprecated for new deploys**. See [SKILL legacy section](./reference.md#legacy-windows-server) and [ON_PREM_WINDOWS_SERVER.md](../../../docs/legacy/ON_PREM_WINDOWS_SERVER.md).

---

## Scenarios (Ubuntu)

| # | Scenario | Signal | Fix |
|---|----------|--------|-----|
| L1 | **openssh-server not installed** | `ssh: connect refused` | Step 1 — `ensure-openssh-server.sh` or `apt install openssh-server` |
| L2 | **sshd not running** | Port 22 closed | `sudo systemctl enable --now ssh` |
| L3 | **ufw blocks 22** | Refused from LAN; OK on localhost | `sudo ufw allow 22/tcp` |
| L4 | **Corporate firewall** | Timeout from LAN | IT: inbound TCP **22** to VM |
| L5 | **Wrong user / no shell account** | Permission denied | Create Linux user with sudo; use `user@host` |
| L6 | **Pubkey auth** | Password disabled | `~/.ssh/authorized_keys` (see [reference.md](./reference.md)) |
| L7 | **Root login disabled** | `root@host` denied | Use sudo-capable deploy user (recommended) |

---

## Script allowlist (strict)

| Script | Step |
|--------|------|
| [ensure-openssh-server.sh](../../../scripts/linux/ensure-openssh-server.sh) | 1 — install + enable `ssh` |

Manifest: [scripts/linux/README.md](../../../scripts/linux/README.md)

### Forbidden

- `scripts/legacy/on-prem-windows/Install-WindowsOpenSshServer.ps1` etc. for **new** Linux hosts (legacy Windows only)
- `scripts/local/**`, `droplet-scripts/**` (droplet has its own SSH/bootstrap)

### Allowed (not repo scripts)

- `ssh`, `scp`, `ssh-copy-id` from admin workstation
- `sudo nano /etc/ssh/sshd_config` (prefer package defaults first)

---

## Goal

| Step | Outcome |
|------|---------|
| **1** | `ssh` service **active**, port **22** listening |
| **2** | `ssh user@<server-ip>` works from admin PC |

**Commands:** [reference.md](./reference.md) · **Experience:** [learnings.md](./learnings.md) · **Maturity:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md)

### Chat openers

- `@.cursor/skills/setup-openssh-server/` — enable SSH on Ubuntu on-prem server
- SSH **connection refused** to Linux VM
- Set up **pubkey** login for deploy user

### Approval mode

One command per message when the user wants **OK** between steps.

---

## Step 0 — Preconditions

- [ ] Ubuntu VM on LAN with sudo
- [ ] Inbound **TCP 22** allowed (host `ufw` + corporate firewall)
- [ ] Linux user for admin (e.g. `deploy` in `sudo` group) — avoid relying on `root` over SSH if disabled

**From admin PC (after Step 1):**

```powershell
Test-NetConnection -ComputerName <server-ip> -Port 22
```

---

## Step 1 — Install OpenSSH Server

**On server (console or existing SSH):**

```bash
sudo bash /opt/visa2026/ensure-openssh-server.sh
```

Or manually ([Ubuntu OpenSSH](https://help.ubuntu.com/community/SSH/OpenSSH/Server)):

```bash
sudo apt-get update
sudo apt-get install -y openssh-server
sudo systemctl enable --now ssh
sudo ufw allow 22/tcp   # if ufw is active
```

**Verify on server:**

```bash
systemctl is-active ssh
ss -tlnp | grep ':22 '
```

**Verify from admin PC:**

```powershell
ssh user@<server-ip>
```

### Step 1 checklist

- [ ] `systemctl is-active ssh` → **active**
- [ ] Port **22** listening
- [ ] Interactive SSH from admin PC works

---

## Step 2 — Pubkey login (recommended)

From **admin PC** (once per user/host):

```powershell
ssh-keygen -t ed25519 -f $env:USERPROFILE\.ssh\id_ed25519_visa_onprem -C visa-onprem
```

```bash
# Linux / Git Bash
ssh-copy-id -i ~/.ssh/id_ed25519_visa_onprem.pub user@<server-ip>
```

Or paste pubkey into `~/.ssh/authorized_keys` on the server (`chmod 600`).

**`~/.ssh/config` on admin PC:**

```sshconfig
Host visa2026-onprem
  HostName 10.100.128.25
  User deploy
  IdentityFile ~/.ssh/id_ed25519_visa_onprem
```

Then: `ssh visa2026-onprem`

### Step 2 checklist

- [ ] Key login works without password (if desired)
- [ ] `scp` works for copying `docker-compose.prod.yml` and `.env.prod` to `/opt/visa2026/`

---

## Agent workflow

1. **Read** [learnings.md](./learnings.md).
2. Confirm target is **Ubuntu** (not Windows) — if Windows only, use [legacy reference](./reference.md#legacy-windows-server).
3. **Step 1** — `ensure-openssh-server.sh` or apt install.
4. Verify port **22** and login from admin PC.
5. **Step 2** — pubkey optional but recommended for deploy automation.
6. Do **not** run Docker scripts from this skill.
7. **Append** [learnings.md](./learnings.md) after verified SSH.

---

## Investigation map

| Signal | Action |
|--------|--------|
| Connection refused | **L1–L3** |
| Timeout | **L4** |
| Permission denied | **L5–L6** |
| Windows `DOMAIN\user` | Wrong OS — use Linux user or legacy Windows doc |
| Docker / compose | [setup-docker-engine](../setup-docker-engine/SKILL.md) |

---

## Continuous improvement

Append-only [learnings.md](./learnings.md); tag entries **linux** or **windows-legacy**. Promotion rules: [MATURITY.md](../on-prem-deploy/MATURITY.md).
