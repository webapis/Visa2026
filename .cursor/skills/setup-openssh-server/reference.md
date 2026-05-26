# setup-openssh-server — command reference (Ubuntu)

Skill: [SKILL.md](./SKILL.md) · Runbook: [docs/ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md) · **Maturity:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md)

Deploy path on server: **`/opt/visa2026/`**

---

## Step 1 — Install (Ubuntu)

```bash
sudo bash /opt/visa2026/ensure-openssh-server.sh
```

**Manual:**

```bash
sudo apt-get update
sudo apt-get install -y openssh-server
sudo systemctl enable --now ssh
```

**If `ufw` is active:**

```bash
sudo ufw allow 22/tcp
sudo ufw status
```

**Check service and port:**

```bash
systemctl status ssh --no-pager
ss -tlnp | grep ':22 '
```

---

## Verify from admin PC

```powershell
Test-NetConnection -ComputerName <server-ip> -Port 22
ssh user@<server-ip>
```

---

## Step 2 — Pubkey auth

**Generate key (admin PC):**

```powershell
ssh-keygen -t ed25519 -f $env:USERPROFILE\.ssh\id_ed25519_visa_onprem -C visa-onprem
```

**Install pubkey on server:**

```bash
ssh-copy-id -i ~/.ssh/id_ed25519_visa_onprem.pub user@<server-ip>
```

**Or on server:**

```bash
mkdir -p ~/.ssh && chmod 700 ~/.ssh
echo '<paste public key line>' >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

**SSH config (`~/.ssh/config` on admin PC):**

```sshconfig
Host visa2026-onprem
  HostName 10.100.128.25
  User deploy
  IdentityFile ~/.ssh/id_ed25519_visa_onprem
```

---

## Copy deploy files over SSH

```powershell
scp docker-compose.prod.yml user@<server-ip>:/opt/visa2026/
scp .env.prod user@<server-ip>:/opt/visa2026/
scp scripts/linux/remote-compose-sql-up.sh scripts/linux/docker-compose.restart.override.yml user@<server-ip>:/opt/visa2026/
```

---

## Hardening (optional, after login works)

```bash
# Disable password auth once pubkey works (edit carefully)
sudo sed -i 's/^#*PasswordAuthentication.*/PasswordAuthentication no/' /etc/ssh/sshd_config
sudo systemctl reload ssh
```

Test **new** SSH session before closing the current one.

---

## Legacy: Windows Server

For **existing Windows Server + Win32 OpenSSH** only (not new deploys):

| Script | Purpose |
|--------|---------|
| `scripts/legacy/on-prem-windows/Install-WindowsOpenSshServer.ps1` | Install `sshd` on Windows |
| `scripts/legacy/on-prem-windows/Repair-WindowsOpenSshServer.ps1` | Connection reset / domain |
| `scripts/legacy/on-prem-windows/Setup-OnPremSshAuthorizedKey.ps1` | `administrators_authorized_keys` |

Login: `ssh DOMAIN\user@host` · Config: `C:\ProgramData\ssh\sshd_config`

Runbook: [ON_PREM_WINDOWS_SERVER.md](../../../docs/legacy/ON_PREM_WINDOWS_SERVER.md)
