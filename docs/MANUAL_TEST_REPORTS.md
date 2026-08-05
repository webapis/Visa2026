# Manual test reports (separate from officer user manual)

Status: **Phase 3 foundation**  
Last updated: 2026-08-04

---

## Purpose

Visa officers read **`user-manual/`** (MkDocs) — guide prose and screenshots only, with an optional **green tick** when verified.

Supervisors, QA, and developers need the **full test matrix**: which suites ran, which tests **passed**, **failed**, or were **not run**. That detail lives in **`manual-test-reports/`**, not in guide Markdown.

| Surface | Audience | Content |
|---------|----------|---------|
| Officer manual | Visa officers | How-to guides, screenshots |
| **Test reports** | Supervisors / QA / devs | Pass/fail/not-run per suite, test, and guide |

---

## Layout

```text
manual-test-reports/
  manifest.yaml     # tracked — registered test suites
  README.md         # tracked
  latest/           # generated (gitignored)
    summary.json
    summary.html
    guides-matrix.md
  runs/             # archived TRX per run (gitignored)
```

---

## Status values

| Status | Meaning |
|--------|---------|
| `passed` | Executed and succeeded |
| `failed` | Executed and failed |
| `not_run` | Registered but not executed in this run |
| `skipped` | Optional suite or explicitly skipped (e.g. `-SkipE2E`) |

**Overall** can be `passed`, `failed`, or `incomplete` (unit tests green but required E2E not run).

---

## Generate locally

```powershell
# After dotnet test with TRX logger
./scripts/ci/Write-ManualTestReport.ps1 -TrxPath TestResults/user-manual-docs.trx

# Run unit suites from manifest + write report (E2E = not_run unless -RunE2E)
./scripts/ci/Write-ManualTestReport.ps1 -RunTests

# Full manual build (unit TRX + report at end)
./scripts/ci/Build-UserManual.ps1 -SkipE2E
```

Preview:

```powershell
./scripts/local/Serve-ManualTestReports.ps1
# → http://127.0.0.1:8766/latest/summary.html
```

---

## Guide linkage

Each English guide under `user-manual/docs/en/` declares `e2eTestFilter` in frontmatter (e.g. `PersonOfficerJourney_LoginCreateEmployeeAddPassport`). The report maps that filter to TRX test names and sets per-guide status.

E2E tests should carry `[Trait("Category", "UserManual")]` so manifest filters stay aligned.

---

## CI

`user-manual.yml` uploads `manual-test-reports/latest/` as an artifact after `Build-UserManual.ps1`.

On-prem: serve `/manual/` and `/manual-test-reports/` on different paths (see [USER_MANUAL_RELEASE.md](USER_MANUAL_RELEASE.md)).

---

## Related

- [testing-evidence.md](../.cursor/skills/visa2026-user-manual/testing-evidence.md) — green tick vs full report
- [USER_MANUAL_PIPELINE.md](USER_MANUAL_PIPELINE.md) — orchestration step 13
- [manual-test-reports/README.md](../manual-test-reports/README.md) — quick commands
