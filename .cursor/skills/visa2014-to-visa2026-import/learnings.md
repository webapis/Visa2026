# Learnings (append-only): visa2014-to-visa2026-import

**Purpose:** Record verified discovery, strategy decisions, mapping corrections, and OData import outcomes so **each session builds on the last**.

**Loop:** [MATURITY.md](./MATURITY.md) — **read `## Entries` before every task**; **append after verified work** (not optional).

**Canonical plan:** [docs/VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md) · [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md)

**Not here:** Visa2026 seed scenarios — [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md). **Import runbook:** [import-practices.md](./import-practices.md).

---

## When to append (required)

| Event | Append? |
|-------|---------|
| Discovery dossier closed (`complete` / `blocked` / `skip`) | **Yes** |
| Excel preview exported or reviewed | **Yes** (note path + row counts) |
| Strategy decision locked or plan approved | **Yes** |
| Pilot or batch reconciled | **Yes** |
| Verified mapping or OData fix | **Yes** |
| Exploratory SQL with no conclusion | No |
| User asked read-only question | No |

Promote repeated patterns into [SKILL.md](./SKILL.md) after **2+** occurrences ([MATURITY.md](./MATURITY.md)).

---

## Entry templates

### Discovery / mapping

```markdown
### YYYY-MM-DD — <TargetODataEntity> — <short title>

- **Phase**: discovery | mapping
- **Dossier**: docs/VISA2014_MIGRATION/discovery/{Entity}.yaml
- **Legacy table(s)**:
- **Symptom / surprise**:
- **SQL / MCP that helped**:
- **Fix / mapping change**:
- **Reconciliation** (if any):
- **Prevent** (next session):
- **Artifacts**: field-map, lookup-translations, inventory
```

### Strategy / plan

```markdown
### YYYY-MM-DD — strategy — <decision title>

- **Phase**: strategy
- **Open decision id** (import-strategy.yaml):
- **Chosen option**:
- **Why**:
- **Artifacts**: IMPORT_PLAN_AND_STRATEGY.md, import-strategy.yaml
```

### Excel preview

```markdown
### YYYY-MM-DD — <TargetODataEntity> — excel preview

- **Phase**: excel-preview
- **Export path**: preview-export/{Entity}-preview.xlsx
- **Counts**: legacy __ → after dedupe __ → main sheet __ → skipped __
- **Surprises** (_UnmappedLookups, bad defaults):
- **Mapping fixes**:
- **Ready for importConfirmed**: yes | no
```

### Pilot / import run

```markdown
### YYYY-MM-DD — <TargetODataEntity> — pilot | batch

- **Phase**: import
- **Environment**: Visa2026DbDev | staging | prod
- **Counts**: legacy SQL __ → target __
- **Skipped / dedupeMerged**:
- **OData errors**:
- **Fix**:
- **Reconciliation pass**: yes | no
```

---

## Entries

_(None yet — append after verified discovery, strategy approval, or pilot import.)_
