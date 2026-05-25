# Learnings (append-only): setup-docker-engine

**Purpose:** Record **try → test → fix** on real hosts so Docker install and compose get faster on the next server.

**Maturity loop:** [on-prem-windows-deploy/MATURITY.md](../on-prem-windows-deploy/MATURITY.md)

**Prerequisites:** [visa2026-windows-server-setup](../visa2026-windows-server-setup/SKILL.md) must finish before this skill runs.

**Not here:** WSL/Ubuntu bootstrap, SSH — other on-prem skills.

**Runbook:** [docs/ON_PREM_WINDOWS_SERVER.md](../../../docs/ON_PREM_WINDOWS_SERVER.md)

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
- **Fix**: Complete **visa2026-windows-server-setup** first; Gate 0 **FAIL=0**.
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
- **Fix**: Build `debs` on internet PC per [reference-docker-offline-install.md](../../../scripts/on-prem/reference-docker-offline-install.md).
- **Prevent**: Offline path only when WSL cannot reach `download.docker.com` (**A3**).
- **Skill**: setup-docker-engine
