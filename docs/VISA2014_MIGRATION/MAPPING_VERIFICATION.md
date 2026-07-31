# VISA2014 → Visa2026 — post-import mapping verification

**Status:** Application + ApplicationProgress shipped — `--verify-visa2014-mapping --entity Application|ApplicationProgress`. Other entities TBD.

**Problem:** `FailedCount = 0` and row-count reconcile prove the wave wrote without errors. They do **not** prove that each target property received the correctly mapped value (especially lookups such as `ApplicationType`).

**Principle:** After each scalar import / reimport wave, verify **expected vs actual** using the **same** transform pipeline as import (`field-maps` + `lookup-translations`). Do not maintain a second hand-written SQL mapping.

**Related:** [LOOKUP_RESOLUTION_STRATEGY.md](LOOKUP_RESOLUTION_STRATEGY.md) (can we map?) · [import-practices.md](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md) · [EXCEL_PREVIEW_EXPORT.md](EXCEL_PREVIEW_EXPORT.md) (pre-write review)

---

## Preflight vs post-import verify

| Gate | Answers | Does not prove |
|------|---------|----------------|
| Lookup preflight (`--preflight-visa2014-lookups`) | Every live legacy value **can** translate; target catalog **has** the key | Rows were written with that key |
| Excel preview | Human sees intended transforms before write | Target DB matches |
| Row-count / id-map reconcile | Volume and FK joinability | Wrong property / wrong lookup value |
| **Mapping verify (this doc)** | Written property values match transform output | — |

Chain order (per BO):

```text
lookup resolution → lookup preflight → import wave (exit 0 + FailedCount 0)
  → mapping verify (exit 0)
  → next order.yaml entity
```

---

## Three verification tiers

### Tier A — Lookup / catalog histograms (cheap, every wave)

For each field with `transform` in `{ lookup, lookup_composite, lookup_identity_passthrough, lookup_nametm, … }` and `verify.tier` ≥ A:

1. Re-run transform for all importable legacy rows → expected catalog key (`lookupMatch`: `Name`, `Code`, `LocalizationKey`, …).
2. Load actual keys from Visa2026 via id-map join (ObjectSpace or SQL).
3. Compare **value → count** distributions (after approved exclusions).

**Pass:** histograms match within exclusion tolerance.  
**Fail example:** all Applications land on `App_Inv` when legacy mix expects many types.

### Tier B — Sampled field parity (every wave)

For N id-map pairs (default **50**, or stratified e.g. 5 per `ApplicationType`):

| Kind | Compare |
|------|---------|
| Scalar | Normalized string / date / number / bool |
| Lookup | Catalog **key** only — never UI caption / localized display |
| FK | Target ID equals id-map resolution of expected legacy FK |

**Pass:** zero mismatches on sampled rows for fields with `verify.enabled: true`.

### Tier C — Full expected-vs-actual (critical BOs / after mapping change)

Same as Tier B for **all** id-map rows. Use after field-map or lookup-overlay edits, before Demo→Prod, and for Application / ApplicationItem / Person when risk is high.

---

## Expected-vs-actual algorithm

```text
for each legacy row in importable set (same filters as import):
  expected = Transform(legacyRow, field-map, lookup-translations)   # shared with import + Excel preview
  targetId = id-map[legacyOid]
  if missing targetId → count as missing_map (fail unless excluded)
  actual = LoadTarget(targetId, verify fields)
  for each field where verify.enabled:
    if Normalize(expected[field]) != Normalize(actual[field]) → mismatch
emit report JSON (+ optional HTML summary)
exit 1 if any Tier A histogram fail OR Tier B/C mismatch OR missing_map beyond exclusions OR unexpected silent (`actual_without_expected`)
```

**Normalization rules (document per transform):**

- Dates: date-only vs DateTimeOffset UTC day
- Strings: trim; optional Unicode NFC
- Lookups: compare `lookupMatch` property value only
- Nulls: honor `missingBehavior` / approved defaults (expected null vs actual default may be OK if field-map says `use_default`)

**Do not** reimplement ApplicationType composite logic in verify SQL — call the same C# transform used by `--import-visa2014` / preview export.

---

## Field-map opt-in (`verify` block)

Extend `fields[]` (and optionally `propertyGaps.targetOnly` derived fields) with:

```yaml
  - source: composite_application_type
    target: ApplicationType
    transform: lookup_composite
    lookupCatalog: ApplicationType
    lookupMatch: Name
    verify:
      enabled: true
      tier: A   # A = histogram; B = sample parity; C = full (implies A+B)
      severity: error   # error | warn
      compare: lookup_key
```

Suggested defaults when `verify` omitted:

| Transform family | Default verify |
|------------------|----------------|
| `lookup*` / composite lookups | `enabled: true`, `tier: A` (histogram always); include in B/C sample |
| Stable scalars (`date`, `string_trim`, number parses) | `enabled: true`, `tier: B` |
| `constant_*`, derived Year/Month, UI-only | `enabled: false` unless explicitly opted in |
| Binary / file waves | Out of scope here — [FILE_AND_IMAGE_IMPORT.md](FILE_AND_IMAGE_IMPORT.md) checksums |

Approved intentional differences: same spirit as [`import-exclusions.yaml`](import-exclusions.yaml) — document why a field may differ; do not silent-pass.

---


## Pilot: ApplicationProgress

Synthetic multi-row BO (`{legacyApplicationOid}:{stepCode}` id-map). Expected from `Visa2014ApplicationProgressTransform.PrepareImportBatch`.

| Target | Verify |
|--------|--------|
| **State** | `ApplicationState.Code` — Tier A histogram + parity |
| Date | date-only — **warn** (synthesis heuristics may drift vs imported rows) |
| Order | **warn** (differs after `--correct-application-progress-order` / AssignTimelineOrders) |
| Description | optional when null — **error** |
| Application | target Application ID via Application id-map — **error** |

Exit fails on State histogram delta, error-severity parity, or **missing id-map** (transform synthesized a step key not in ApplicationProgress.json — usually means reimport after transform change). Id-map may also contain older keys no longer synthesized (orphans are not counted as fail).

HTML/JSON include a **Property lineage** catalog (destination ← legacy source) and a **Sample row lineage** table (per-step Date/Description sources from transform _lineage_* fields).

Expected synthesis **must** use the same ministry-leg counts as import (Visa2014ApplicationMinistryLegCountResolver from target Applications). Rows with no Application id-map are counted as skips (same as import), not missingIdMap failures.

Silent / implicit outcomes: **not** inventoried for ApplicationProgress v1 (synthesis codes, not YAML lookup composites).

```powershell
dotnet run --project Visa2026.DataImporter -- `
  --verify-visa2014-mapping `
  --entity ApplicationProgress `
  --legacy-source calik-energi-local-pg `
  --target-connection "..." `
  --tier B --sample 50
```

## Pilot: Application (first implementation)

Highest-signal fields from [`field-maps/Application.yaml`](../../Visa2026.DataImporter/legacy/visa2014/field-maps/Application.yaml):

| Target | Why verify |
|--------|------------|
| **ApplicationType** | Composite layer-3 key (`App_Inv`, …) — classic silent-wrong mapping |
| Urgency, VisaPeriod, VisaCategory | Lookup composites |
| FullApplicationNumber, ApplicationDate | Stable scalars / upsert identity |
| ProjectContract | Identity passthrough when present |

Example assert path for ApplicationType:

1. Legacy composite key → `lookup-translations` → expected `Name` (e.g. `App_Inv`)
2. Target `Application.ApplicationType.Name` (via navigation or join) must equal that key
3. Tier A: count of each `Name` on expected set == count on imported set (id-map scoped)

---

## CLI

```powershell
dotnet run --project Visa2026.DataImporter -- `
  --verify-visa2014-mapping `
  --entity Application `
  --legacy-source calik-energi-local-pg `
  --target-connection "..." `
  --application-id-map "legacy/visa2014/id-maps/.../Application.json" `
  --sample 50
  # --full          # Tier C
  # --tier A        # histograms only
  # --report path/to/Application-mapping-verify.json
```

| Flag | Role |
|------|------|
| `--entity` | `Application` or `ApplicationProgress` |
| `--sample N` | Tier B sample size (ignored if `--full`) |
| `--full` | Tier C all id-map rows |
| `--tier A\|B\|C` | Minimum tier to run (default B = A+B) |
| `--report` | JSON report path (under `import-logs/` by default; gitignored PII) |
| `--report-html` | Optional HTML path (default: same basename as `--report` with `.html`) |

**Orchestrators:** after each successful wave in `OnPrem-Sync.ps1` / `Run-HeadlessChain.ps1` / local PG chain, run verify for that entity; halt chain on non-zero exit (same as FailedCount). Optional `-SkipMappingVerify` only for approved debug.

**Wrapper:** `scripts/visa2014-migration/import/Verify-Mapping.ps1` — thin CLI wrapper; prefer C# in DataImporter over duplicate PowerShell compare logic.

---


## Silent / implicit outcomes (Application v1)

Same `--verify-visa2014-mapping` run inventories **how** each lookup value was produced (not a separate CLI).

**Gate:** fail only on **unexpected** silent buckets. Documented defaults and explicit YAML remaps are **info** and do not flip exit code by themselves.

| Bucket | Meaning | Exit impact |
|--------|---------|-------------|
| `explicit_yaml` | Hit `lookup-translations` `values[]` (exact key) | info |
| `normalized_yaml` | Matched YAML via NormalizeKey fold | info |
| `identity_passthrough` | Catalog `identityPassThrough` | info |
| `default_applied` | Transform / `use_default` / hard-coded default (e.g. Urgency→`NORM`) | info |
| `null_allowed` | Expected null and actual null/empty | info |
| `actual_default_tolerated` | Expected null, actual equals known BO/catalog default (`Month6` / `Multiple` / `NORM`) when field left null by design | info |
| `actual_without_expected` | Expected null, actual is a **non-default** value | **fail** |
| `skipped_unmapped` | Transform skipped row (run-level count on ApplicationType) | info |
| `mismatch` | Parity failure (existing Tier B/C) | **fail** |

Classifier: `Visa2014LookupOutcomeClassifier` (same resolve order as `Visa2014LookupTranslator.TryTranslate`). JSON/HTML include a **Silent / implicit outcomes** section with per-field bucket counts and unexpected samples.

---
## Report outputs

Each run writes **JSON** and **HTML** side by side (PASS/FAIL badge, summary cards, histogram tables, mismatch table). Open the .html in a browser.

## Report shape (sketch)

```json
{
  "entity": "Application",
  "legacySource": "calik-energi-local-pg",
  "tier": "B",
  "sampled": 50,
  "idMapCount": 12247,
  "histograms": [
    {
      "field": "ApplicationType",
      "ok": true,
      "expected": { "App_Inv": 4100, "App_Inv_And_WP": 800 },
      "actual": { "App_Inv": 4100, "App_Inv_And_WP": 800 },
      "delta": {}
    }
  ],
  "mismatches": [
    {
      "legacyOid": "...",
      "targetId": "...",
      "field": "ApplicationType",
      "expected": "App_Visa_Ext",
      "actual": "App_Inv"
    }
  ],
  "missingIdMap": 0,
  "silentFields": [
    {
      "field": "ApplicationType",
      "buckets": { "explicit_yaml": 12000, "default_applied": 0, "skipped_unmapped": 12 }
    }
  ],
  "silentUnexpectedSamples": [],
  "exitCode": 0
}
```

Append a one-line summary to [learnings.md](../../.cursor/skills/visa2014-to-visa2026-import/learnings.md) after each verify run (pass or fail).

---

## Anti-patterns

- Parallel “verify SQL” that reimplements `lookup_composite` / ApplicationType rules
- Comparing localized captions or ListView display text
- Treating `Compare-OnPremImportRuns` (reimport Δ) as mapping correctness
- Skipping verify on Demo/Prod full Import while relying only on FailedCount
- Failing Tier B on fields with `verify.enabled: false` or approved exclusion

---

## Implementation checklist (DataImporter)

1. Shared transform already powers import + preview — expose a **read-only expected payload** API per entity (no writes).
2. Add `Visa2014MappingVerifyCommand` (`--verify-visa2014-mapping`).
3. Load actuals via headless ObjectSpace (`--inprocess`) or read-only SQL against target (prefer ObjectSpace for navigation properties).
4. Opt-in `verify:` on Application field-map; run local pilot on Application after next partial reimport.
5. Wire into orchestrators; document in skill scripts table + [scripts/visa2014-migration/README.md](../../scripts/visa2014-migration/README.md).
6. Promote stable flags to [reference.md](../../.cursor/skills/visa2014-to-visa2026-import/reference.md) after 2+ successful gated runs.

---

## Acceptance for “mapping verify shipped”

- [x] `--verify-visa2014-mapping --entity Application` exits 0 on a known-good local import (local PG smoke, max-rows 200)
- [ ] Deliberate wrong ApplicationType translation fails Tier A or B with clear mismatch rows (manual QA)
- [x] `OnPrem-Sync.ps1` runs Application mapping verify after wave success (halt unless `-SkipMappingVerify`)
- [x] Skill + import-practices point here; learnings entry for design + ship