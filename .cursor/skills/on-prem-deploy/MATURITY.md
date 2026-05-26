# On-prem deploy skills — continuous improvement (maturity)

**Ubuntu** (current) and **legacy Windows + WSL** skills share one **experience loop**. Each run should make the **next** deploy on a similar host **faster and more deterministic**.

| Skill | Folder | `learnings.md` |
|-------|--------|----------------|
| **legacy-on-prem-windows-setup** | [../legacy-on-prem-windows-setup/](../legacy-on-prem-windows-setup/SKILL.md) | [learnings.md](../legacy-on-prem-windows-setup/learnings.md) |
| **setup-openssh-server** | [../setup-openssh-server/](../setup-openssh-server/SKILL.md) — **Ubuntu SSH** (Win32 legacy in reference) | [learnings.md](../setup-openssh-server/learnings.md) |
| **setup-docker-engine** | [../setup-docker-engine/](../setup-docker-engine/SKILL.md) — **Ubuntu on-prem** (was WSL) | [learnings.md](../setup-docker-engine/learnings.md) |

**Runbook (narrative):** [docs/ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md) (current) · [docs/legacy/ON_PREM_WINDOWS_SERVER.md](../../../docs/legacy/ON_PREM_WINDOWS_SERVER.md) (legacy) · **Prereqs:** [docs/ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md)

---

## Maturity goal

| As usage increases | Effect |
|--------------------|--------|
| More **try / test / fix** cycles captured | Fewer repeated dead-ends on new hosts |
| Entries promoted into **SKILL.md** / **scenarios** | Agents start with the right command and skip known-bad paths |
| **reference.md** gains host-specific notes | Copy-paste commands stay stable across servers |
| Same symptom twice on different hosts | Becomes a **scenario row** or investigation-map line — deterministic triage |

**Starter-friendly:** a new agent (or human) opens the skill, reads **learnings** + **scenarios**, and follows steps that already worked on `10.100.128.25`-class servers.

---

## The loop (every task)

```text
1. READ   → skill learnings.md (## Entries) + scenario tables in SKILL.md
2. TRY    → one allowlisted command; user OK between steps if requested
3. TEST   → verify (prereq script, docker hello-world, ssh handshake, HTTP)
4. FIX    → only allowlisted scripts / documented wsl/bash fallbacks
5. RECORD → append learnings.md (verified fix only)
6. PROMOTE → if pattern repeats, lift into SKILL.md (see ladder below)
```

**Record when:** the fix was **verified** on a real host (not speculative). Include **server IP or role** when useful (e.g. `10.100.128.25`, domain `CALIKSOA`).

**Do not:** delete or rewrite old learnings entries; **append only**.

---

## Entry template (copy into the skill’s `learnings.md`)

```markdown
### YYYY-MM-DD — <short title> (<host or context>)

- **Symptom**:
- **Try**:
- **Test**:
- **Fix**:
- **Prevent**:
- **Skill**: legacy-on-prem-windows-setup | setup-openssh-server | setup-docker-engine
```

---

## Promotion ladder (skill gets “more skilled”)

| Hits | Action |
|------|--------|
| **1** verified fix | Append **learnings.md** only |
| **2** hosts or 2+ chats with same root cause | Add one row to **Scenarios** table in **SKILL.md** (or tighten **Investigation map**) |
| **3+** or blocks every new server | Add a **Step checklist** bullet, **reference.md** command block, or **Agent workflow** rule |
| Stable for months | Optional one-line pointer in [docs/legacy/ON_PREM_WINDOWS_SERVER.md](../../../docs/legacy/ON_PREM_WINDOWS_SERVER.md) troubleshooting table |

**Developer review:** promoted text in **SKILL.md** should stay short; long command logs stay in **reference.md** or **learnings.md**.

**Cross-skill lessons:** if the lesson belongs to another skill (e.g. WSL **Stopped** during Docker), append under the skill where you **fixed** it and add a one-line cross-link in the other skill’s learnings (“see setup-docker-engine entry …”).

---

## Which skill owns the entry?

| Work done | Log to |
|-----------|--------|
| Prereq audit, WSL, Ubuntu, systemd, `.wslconfig` | **legacy-on-prem-windows-setup** |
| `sshd`, domain SSH, port 22, connection reset | **setup-openssh-server** |
| Docker Engine install, compose, images, HTTP app | **setup-docker-engine** |

---

## Agent obligations (all three skills)

1. **Before** starting steps on a host: read that skill’s **learnings.md** (at least recent entries + search symptom keywords).
2. **During** approval mode: one command per message; state which **scenario ID** you are addressing when known (e.g. **C4**, **S6**).
3. **After** user confirms success: append **learnings.md** in the same chat if there is a new verified lesson.
4. **Never** skip **Gate 0** / skill-order rules to save time — that creates duplicate learnings and slower future runs.

---

## Human maintainer (optional)

Periodically skim all three `learnings.md` files and batch-promote repeated patterns into **SKILL.md** so the agent does not need to read long logs every time.
