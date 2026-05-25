# setup-openssh-server — command reference

Skill: [SKILL.md](./SKILL.md) · Runbook: [docs/ON_PREM_WINDOWS_SERVER.md](../../../docs/ON_PREM_WINDOWS_SERVER.md) · **Maturity:** [on-prem-windows-deploy/MATURITY.md](../on-prem-windows-deploy/MATURITY.md)

Scripts on server: `C:\visa2026-deploy\`

---

## Step 1 — Install

```powershell
cd C:\visa2026-deploy
.\Install-WindowsOpenSshServer.ps1
```

| Parameter | When |
|-----------|------|
| `-ZipPath C:\Temp\OpenSSH-Win64.zip` | No `github.com` on server |
| `-Port 22` | Non-default SSH port |
| `-SkipFirewall` | Firewall managed by GPO/IT (open port separately) |
| `-ForceWin32BinaryInstall` | Service exists but binaries incomplete |

**Check service and port:**

```powershell
Get-Service sshd | Format-Table Status, Name, StartType
Get-NetTCPConnection -LocalPort 22 -State Listen
```

---

## Verify (server or admin PC)

**On server:**

```powershell
.\Test-OnPremServerPrerequisites.ps1
```

**From admin PC (LAN + port 22):**

```powershell
.\Test-OnPremServerPrerequisites.ps1 -ServerIp 10.100.128.25
Test-NetConnection -ComputerName 10.100.128.25 -Port 22
```

---

## Step 2 — Repair

```powershell
.\Repair-WindowsOpenSshServer.ps1 -TestUser adm43419
```

| Parameter | When |
|-----------|------|
| `-ReinstallWin32OpenSsh` | Missing `sshd-session.exe` / broken install |
| `-ZipPath ...` | Offline reinstall |
| `-DefaultShell PowerShell` | Prefer PowerShell over cmd for SSH sessions |
| `-SkipHandshakeTest` | Config-only pass |

**Config test:**

```powershell
C:\Windows\System32\OpenSSH\sshd.exe -t -f C:\ProgramData\ssh\sshd_config
```

**Domain login test (admin PC):**

```powershell
ssh -o PreferredAuthentications=password -o PubkeyAuthentication=no DOMAIN\adm43419@10.100.128.25
```

---

## Domain-joined diagnostics (on server)

Repair prints:

- `sshd_config` **Match** blocks
- **OpenSSH/Admin** event log errors
- `Administrator@127.0.0.1` handshake (isolates domain vs sshd)
- Suggested **IT** checks (Allow log on locally, AD lockout)

---

## Copy scripts to server (first time)

From repo (USB, RDP paste, or `scp` after SSH works):

- `Install-WindowsOpenSshServer.ps1`
- `Repair-WindowsOpenSshServer.ps1`
- `Test-OnPremServerPrerequisites.ps1` (optional)

Target folder: `C:\visa2026-deploy\`
