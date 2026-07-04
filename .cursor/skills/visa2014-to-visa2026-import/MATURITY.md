# VISA2014 import skill — maturity loop

This skill **accumulates experience** from every migration session — especially **import runs** (success, failure, or partial success). Same pattern as [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md).

## Loop (every session)

1. **READ** [learnings.md](./learnings.md) (`## Entries`) — newest first; search for the BO, script, or error you are about to hit.
2. **READ** [scripts/visa2014-migration/README.md](../../../scripts/visa2014-migration/README.md) — **reuse** an existing script or CLI before creating a new `.ps1`.
3. **READ** [migration-status.yaml](../../../docs/VISA2014_MIGRATION/migration-status.yaml) — `currentFocus`, `issues`, entity `importStatus`.
4. **READ** [import-strategy.yaml](../../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) — `status` must be `approved` before `--import-visa2014` implementation.
5. **WORK** — discovery, strategy, Excel preview, OData import, partial reimport, correction CLI, or file wave.
6. **VERIFY** — SQL reconciliation, spot-checks, id-map counts, log tail (when applicable).
7. **UPDATE** [migration-status.yaml](../../../docs/VISA2014_MIGRATION/migration-status.yaml) when a workstream advances or a **blocking** issue appears.
8. **APPEND** [learnings.md](./learnings.md) — **required after every import attempt** (see below). Also append when a dossier closes, strategy locks, or a mapping fix is verified without a full import.
9. **PROMOTE** — repeated issue → Troubleshooting in [SKILL.md](./SKILL.md); stable procedure → [reference.md](./reference.md) or [import-practices.md](./import-practices.md).

## When to append learnings (import runs)

| Situation | Append? | Notes |
|-----------|---------|--------|
| End-to-end or single-entity import **succeeds** | **Yes** | Counts, reconciliation, log path, script/CLI used |
| Import **fails** (non-zero exit, OData error, validation) | **Yes** | Exit code, error snippet, root cause or hypothesis, next step |
| Import **partial** (some rows skipped/failed) | **Yes** | Success/failed/skipped counts; sample failures |
| Partial reimport (dev `reimport/` script) | **Yes** | Cleanup + id-map rebuild + import; target DB |
| Correction CLI only (`--correct-*`) | **Yes** | Rows updated; before/after spot-check |
| File/image wave | **Yes** | Bytes loaded, missing id-map keys |
| Discovery / Excel preview only (no import) | Optional | Append if non-obvious SQL or mapping insight |
| Build failed before import ran | **Yes** | Compiler error; blocks repeat attempts |

**Do not skip failure entries.** They are often more valuable than success entries.

## What to log (import entries)

Use the templates in [learnings.md](./learnings.md). Minimum fields:

- **Date**, **environment** (local / staging / prod pilot), **tenant** if applicable
- **Mode**: end-to-end | single-entity | partial-reimport | correction | file-wave
- **Entity / BO** and **script or CLI** (e.g. `scripts/visa2014-migration/reimport/ApplicationItems.ps1`)
- **Outcome**: success | failed | partial
- **Counts**: legacy source, imported, failed, skipped (as available)
- **Reconciliation** (one line) or **error** (snippet + exit code)
- **Log path** (e.g. `import-logs/…`) when the run produced one
- **Follow-up** — fix applied, next run, or `migration-status.yaml` issue id

## Promotion rules

| Hits | Action |
|------|--------|
| Same failure or workaround **2×** | Add row to Troubleshooting in [SKILL.md](./SKILL.md) |
| Same procedure **3×** verified | Move steps to [reference.md](./reference.md) or [import-practices.md](./import-practices.md) |
| Strategy / wave decision locked | Update [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) or import-strategy notes |

## Agent obligation

When the user or agent **runs** any import-related script or DataImporter CLI for VISA2014 migration, the session is not complete until step 7 (append learnings) is done — **including failed runs the user pastes logs for**.
