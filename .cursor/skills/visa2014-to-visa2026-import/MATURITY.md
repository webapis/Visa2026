# VISA2014 → Visa2026 import — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) (append-only) · **Commands:** [reference.md](./reference.md)

**Canonical docs:**

| Doc | Role |
|-----|------|
| [docs/VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md) | Migration overview, gates, data quality |
| [docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) | **Import plan — approve before implementation** |
| [import-strategy.yaml](../../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) | Strategy approval status |

**Related skills:**

| Topic | Skill / log |
|-------|-------------|
| Target OData / seed patterns | [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) |
| Lookup catalogs | [visa2026-lookup-data](../visa2026-lookup-data/SKILL.md) |
| Docker / DB / schema | [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md) |

Shared promotion rules: [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)

---

## Maturity goal

| As usage increases | Effect |
|--------------------|--------|
| More sessions in **learnings.md** | Next discovery/import skips repeated MCP, mapping, OData mistakes |
| Same root cause **2+** times | Scenario row in **SKILL.md** or triage table |
| Stable SQL / CLI pattern **3+** times | Snippet in **reference.md** |
| Strategy or wave decision locked | Update **IMPORT_PLAN_AND_STRATEGY.md** + **import-strategy.yaml** |

**Developer review:** promoted **SKILL.md** text stays short; long SQL, stack traces, and session notes stay in **learnings.md**.

---

## The loop (every migration task)

Agents **must** follow this order:

```text
1. READ    → learnings.md (## Entries) + import-strategy.yaml status + Scenarios in SKILL.md
2. CLASSIFY → discovery vs strategy vs confirmation vs import implementation vs pilot run
3. GATE    → strategy approved? importConfirmed? (stop if not)
4. TRY     → smallest unit: one BO, one SQL probe, one OData POST
5. TEST    → reconcile counts; dotnet build if code changed
6. FIX     → minimal diff; mapping YAML before code when possible
7. RECORD  → append learnings.md (verified outcome only)
8. PROMOTE → 2+ hits → SKILL.md scenario; 3+ → reference.md
```

**Record when:** discovery dossier closed, strategy decision made, confirmation review, pilot/batch reconciled, or verified fix — not speculative notes.

**Do not:** delete or rewrite old learnings entries; **append only**.

**Agents must not** set `import-strategy.yaml` `status: approved` or `importConfirmed: true` unless the user explicitly approves in session.

---

## What to log (minimum)

| Session type | Required learnings fields |
|--------------|---------------------------|
| Discovery BO closed | Entity, legacy table, surprise gaps, SQL that helped |
| Strategy / plan | Decision id, chosen option, link to IMPORT_PLAN |
| Pilot import | Entity, counts (legacy vs target), skips, OData errors |
| Mapping fix | Layer 1/2/3 file, before/after, reconciliation delta |

Use the template in [learnings.md](./learnings.md).

---

## Promotion ladder

| Hits | Action |
|------|--------|
| **1** verified outcome | Append **learnings.md** only |
| **2** same root cause | Add/update **Troubleshooting** or scenario in **SKILL.md** |
| **3+** same pattern | Command/SQL block in **reference.md** |
| Strategy or wave change | **IMPORT_PLAN_AND_STRATEGY.md** + **import-strategy.yaml** |
| Cross-cutting OData exposure | Note in **visa2026-dataimporter/learnings.md** if that skill owns the fix |

---

## Which log owns the entry?

| Symptom / work | Log to |
|----------------|--------|
| Legacy table/column mapping, VISA2015 SQL, dedupe, id-map | **visa2014-to-visa2026-import** |
| Visa2026 seed scenarios, `--import-scenario` | **visa2026-dataimporter** |
| ApplicationType / catalog seed drift | **visa2026-lookup-data** |
| Target DB schema / Docker / FORCE_XAF_DB_UPDATE | **visa2026-lifecycle-docker** |

When unsure: log where you **changed artifacts or code**; cross-link the other skill in **Prevent**.
