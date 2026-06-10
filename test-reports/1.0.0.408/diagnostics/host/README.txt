Visa2026 UI scenario CI diagnostics
===================================

Download this folder from the GitHub Actions run (artifact: ui-scenario-ci-logs).

Files:
  manifest.txt           - run metadata
  system-diagnostics.txt - port/process/HTTP snapshot at collection time
  wait-diagnostics.log   - wait-loop trace (each HTTP attempt)
  ui-scenario-out.log    - Blazor host stdout
  ui-scenario-err.log    - Blazor host stderr
  scenario-runner.log    - UiScenarioRunner output (if scenarios ran)

Share the whole zip with the agent for triage.
