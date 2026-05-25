# Learnings (append-only): setup-openssh-server

**Purpose:** Record **try → test → fix** for OpenSSH on Windows Server (workgroup and **domain-joined**) so repeat setups take less time.

**Maturity loop:** [on-prem-windows-deploy/MATURITY.md](../on-prem-windows-deploy/MATURITY.md)

**Not here:** WSL, Docker, compose — other on-prem skills.

## Entry template

```markdown
### YYYY-MM-DD — <short title> (<host or context>)

- **Symptom**:
- **Try**:
- **Test**:
- **Fix**:
- **Prevent**:
- **Skill**: setup-openssh-server
```

Promote to [SKILL.md](./SKILL.md) **scenarios** after **2+** hosts.

---

## Entries

### 2026-05-25 — Domain user connection reset (CALIKSOA-style)

- **Symptom**: `ssh DOMAIN\user@server` accepts host key then **Connection reset**; RDP works.
- **Try**: Password SSH as domain admin without repair script.
- **Test**: `Repair-WindowsOpenSshServer.ps1 -TestUser <shortname>`; `Administrator@127.0.0.1` reaches auth.
- **Fix**: Repair sets GSSAPI off, StrictModes off, ACL fixes; client uses `DOMAIN\user` from script output.
- **Prevent**: Run repair after install when account is in local **Administrators** (**S6**).
- **Skill**: setup-openssh-server

---

### 2026-05-25 — Capability installed but no sshd service

- **Symptom**: `Get-WindowsCapability` shows OpenSSH.Server **Installed**; no `sshd` service.
- **Try**: Assume capability alone is enough.
- **Test**: `Get-Service sshd` missing.
- **Fix**: `Install-WindowsOpenSshServer.ps1` (Win32 zip registers service).
- **Prevent**: Scenario **S2** in SKILL.md.
- **Skill**: setup-openssh-server

---

### 2026-05-25 — Administrator test isolates domain

- **Symptom**: Only domain deploy account fails.
- **Try**: Only domain user SSH tests.
- **Test**: `Administrator@127.0.0.1` → Permission denied (auth OK) vs domain reset.
- **Fix**: IT: **Allow log on locally** on server, AD lockout, password expiry.
- **Prevent**: Document domain section in SKILL.md (**S9**).
- **Skill**: setup-openssh-server
