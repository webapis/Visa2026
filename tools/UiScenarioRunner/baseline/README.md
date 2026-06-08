# UI scenario lookup baseline (LocalDB)

Optional speed-up for local **`Invoke-UiScenarioRun.ps1 -All -UseBaselineSnapshot`**.

| File | Purpose |
|------|---------|
| `Visa2026UiScenario-lookup-baseline.bak` | LocalDB backup after greenfield seed (LookupCatalogs + `StandardUser`). **Gitignored** — create on your machine. |

## Create or refresh

From repo root (after a successful build):

```powershell
.\scripts\local\New-UiScenarioBaselineSnapshot.ps1
```

Re-run when:

- Module schema / `AssemblyVersion` changes
- `LookupCatalogs` or tenant manifest changes (e.g. new subcontractor row)
- Baseline restore fails or scenarios miss expected lookup labels

## Use

```powershell
.\scripts\local\Invoke-UiScenarioRun.ps1 -All -UseBaselineSnapshot
```

If the `.bak` is missing, the run script falls back to greenfield `--updateDatabase` (slower, always correct).

**CI** does not use the baseline — each GitHub Actions job gets a fresh LocalDB and greenfield host startup.
