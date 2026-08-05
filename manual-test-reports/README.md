# Manual test reports (separate from officer user manual)

**Audience:** supervisors, QA, developers — **not** visa officers.

The officer manual (`user-manual/`) shows only a **green tick** when a guide is verified. Full pass/fail detail lives here.

| Surface | URL / path |
|---------|------------|
| Officer manual | `/manual/` — prose + screenshots only |
| **Test results** | `/manual-test-reports/` or `manual-test-reports/latest/summary.html` |

## Generate

```powershell
# Report from existing TRX files (after dotnet test)
./scripts/ci/Write-ManualTestReport.ps1 -TrxPath @(
  'TestResults/user-manual-docs.trx',
  'TestResults/user-manual-e2e.trx'
)

# Run unit tests + write report (E2E marked not_run unless -RunE2E)
./scripts/ci/Write-ManualTestReport.ps1 -RunTests

# Full pipeline (inside Build-UserManual.ps1 when E2E is enabled)
./scripts/ci/Build-UserManual.ps1
```

## Local preview

```powershell
./scripts/local/Serve-ManualTestReports.ps1
# → http://127.0.0.1:8766/latest/summary.html
```

## Output layout

```text
manual-test-reports/
  manifest.yaml          # tracked — suite registry
  README.md              # tracked
  latest/                # generated (gitignored)
    summary.json
    summary.html
    guides-matrix.md
  runs/                  # archived TRX per run (gitignored)
```

## Status values

| Status | Meaning |
|--------|---------|
| `passed` | Test executed and succeeded |
| `failed` | Test executed and failed |
| `not_run` | Listed in manifest but not executed in this run |
| `skipped` | Explicitly skipped (optional suite or `-SkipE2E`) |

Canonical design: [testing-evidence.md](../.cursor/skills/visa2026-user-manual/testing-evidence.md) · [MANUAL_TEST_REPORTS.md](../docs/MANUAL_TEST_REPORTS.md)
