# Learnings (append-only): legacy-on-prem-windows-setup

**Purpose:** Record **try → test → fix** on real hosts so the next run is faster. Agents **read before** work; **append after** verified fixes.

**Maturity loop (all on-prem skills):** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md)

**Canonical requirements:** [docs/ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md)

**Not here:** Docker package install, compose, SSH — [setup-docker-engine](../setup-docker-engine/learnings.md), [setup-openssh-server](../setup-openssh-server/learnings.md).

## Entry template

```markdown
### YYYY-MM-DD — <short title> (<host or context>)

- **Symptom**:
- **Try**:
- **Test**:
- **Fix**:
- **Prevent**:
- **Skill**: legacy-on-prem-windows-setup
```

Promote to [SKILL.md](./SKILL.md) **scenarios** after **2+** hosts with the same root cause.

---

## Entries

### 2026-05-25 — Scenario catalog in SKILL.md

- **Symptom**: Agent or user unsure why Docker install cannot start.
- **Try**: Run prereq script without `-RequireDocker`.
- **Test**: FAIL=0 on WSL + systemd; Docker WARN OK.
- **Fix**: Use **SKILL.md** section *Scenarios that hinder Docker Engine setup* (groups **A–F**).
- **Prevent**: Run `Test-OnPremServerPrerequisites.ps1` after each fix.
- **Skill**: legacy-on-prem-windows-setup

---

### 2026-05-25 — Prereq script: Docker not installed is OK for this skill

- **Symptom**: `Test-OnPremServerPrerequisites.ps1` **FAIL** on Docker before WSL bootstrap finished.
- **Try**: Ran script with default flags during Step 0.
- **Test**: Re-run without `-RequireDocker`.
- **Fix**: Docker **WARN** is expected until **setup-docker-engine**.
- **Prevent**: Use `-RequireDocker -RequireDeployFiles` only before compose.
- **Skill**: legacy-on-prem-windows-setup

---

### 2026-05-25 — sshd not required for Docker on server

- **Symptom**: **FAIL** on sshd blocked “ready” message though RDP works.
- **Try**: Treated sshd as blocking gate.
- **Test**: sshd is **WARN** in prereq script.
- **Fix**: Remote admin optional (**setup-openssh-server**).
- **Prevent**: This skill gates on WSL + systemd, not port 22.
- **Skill**: legacy-on-prem-windows-setup

---

### 2026-05-25 — WSL **Stopped** after compose worked once

- **Symptom**: Containers **Exited**; `wsl -l -v` → **Stopped**.
- **Try**: `docker compose up` without checking WSL state.
- **Test**: Ubuntu **Stopped** in `wsl -l -v`.
- **Fix**: `vmIdleTimeout=-1` in `C:\Users\<user>\.wslconfig` for the user running `wsl`.
- **Prevent**: Step 1c in **SKILL.md** on every production host. See also **setup-docker-engine** learnings.
- **Skill**: legacy-on-prem-windows-setup

---

### 2026-05-25 — `Install-WslDockerEngine.ps1 -SkipDockerInstall`

- **Symptom**: User ran full install script during “server prep” and waited 30+ min for Docker apt.
- **Try**: Full `Install-WslDockerEngine.ps1` without `-SkipDockerInstall`.
- **Test**: apt activity in WSL during server-prep phase.
- **Fix**: Windows server setup uses **`-SkipDockerInstall`** only.
- **Prevent**: Skill allowlist documents the flag.
- **Skill**: legacy-on-prem-windows-setup
