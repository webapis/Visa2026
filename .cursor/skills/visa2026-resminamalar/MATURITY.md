# Resminamalar skill — continuous improvement (maturity)

Each verified fix on **catalog, dialog UX, preview, batch ZIP, seed gate, or permissions** should make the **next** Resminamalar incident **faster to diagnose**.

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) (append-only) · **File map:** [reference.md](./reference.md)

**Canonical doc (narrative):** [docs/APPLICATION_REPORT_PACKAGE.md](../../../docs/APPLICATION_REPORT_PACKAGE.md)

**Related experience logs:**

| Topic | Log |
|-------|-----|
| DocxTemplater merge, `RowNo`, Sanaw rows, Extract/Validate, maps | [visa2026-user-report-templates/learnings.md](../visa2026-user-report-templates/learnings.md) |
| Role / navigation denied | [visa2026-security-access/learnings.md](../visa2026-security-access/learnings.md) |
| Docker / schema / `FORCE_XAF_DB_UPDATE` | [visa2026-lifecycle-docker/SKILL.md](../visa2026-lifecycle-docker/SKILL.md) |

Shared promotion rules (doc → skill funnel): [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)

**User chat prompts:** [prompts.md](./prompts.md)

---

## Maturity goal

| As usage increases | Effect |
|--------------------|--------|
| More **try / test / fix** cycles in **learnings.md** | Agents skip repeated dead-ends (empty catalog, preview≠ZIP, batch schema) |
| Same symptom **2+** times | Row added to **Scenarios** in **SKILL.md** — deterministic first step |
| Stable pattern **3+** times | Short rule in **SKILL.md** or command block in **reference.md** |
| UX / officer workflow change | Update **APPLICATION_REPORT_PACKAGE.md** + one learnings entry |

**Developer review:** promoted **SKILL.md** text stays short; long stack traces and SQL stay in **learnings.md** or **reference.md**.

---

## The loop (every Resminamalar task)

Agents **must** follow this order:

```text
1. READ   → learnings.md (## Entries) + Scenarios table in SKILL.md
2. CLASSIFY → Resminamalar surface vs template merge (route to user-report-templates if DocxTemplater token error)
3. TRY    → smallest repro: catalog row? preview? ZIP? worker log?
4. TEST   → dotnet build; manual or described officer path; batch toast / app logs
5. FIX    → minimal diff; keep preview/ZIP parity on ApplicationWordReportEntryGenerator
6. RECORD → append learnings.md (verified fix only)
7. PROMOTE → if same root cause again, update Scenarios / triage in SKILL.md (see ladder)
```

**Record when:** fix verified in running app (or CI if E2E added later) — not speculative.

**Do not:** delete or rewrite old learnings entries; **append only**.

---

## Entry template (copy into learnings.md)

```markdown
### YYYY-MM-DD — <short title> (<Application | ApplicationItem | seed | batch>)

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: resminamalar | user-report-templates | security-access | lifecycle-docker | —
```

---

## Promotion ladder

| Hits | Action |
|------|--------|
| **1** verified fix | Append **learnings.md** only |
| **2** chats or environments with **same root cause** | Add/update one row in **Scenarios** (SKILL.md) |
| **3+** or blocks many officers | Add **Triage** line, **reference.md** snippet, or **Agent workflow** bullet in SKILL.md |
| Officer-facing behaviour change | Update **docs/APPLICATION_REPORT_PACKAGE.md** |

---

## Which skill owns the entry?

| Symptom / work | Log to |
|----------------|--------|
| Empty **User Report Template** list, seed gate, catalog empty, scope (App vs Item), gear, preview PDF, toast, `SelectedReportKeysJson`, worker ZIP assembly | **visa2026-resminamalar** |
| `'{{…}}' could not be replaced`, wrong row shape, Sanaw/Forma_16 rows, Extract/Validate, map tokens | **visa2026-user-report-templates** (cross-link from resminamalar learnings) |
| “Prohibited by security rules” on template BO (not host) | **security-access** or resminamalar if Resminamalar-only host |
| `Invalid column name` on batch tables, Docker deploy | **lifecycle-docker** |

When unsure: log under the skill where you **changed code**; add **Cross-skill** pointer in the other folder’s learnings if officers see it from Resminamalar.

---

## Scenario promotion queue

When an entry below reaches **2+** verified hits, promote to **SKILL.md → Scenarios**:

| Candidate | Current home |
|-----------|----------------|
| Empty template list after deploy | learnings 2026-06-06 seed gate |
| Check chip vs hard ZIP failure | learnings 2026-06-06 readiness |
| Extract security on placeholder grid | learnings 2026-06-06 Extract |
| Preview OK / ZIP fail (selection JSON) | SKILL triage table |
