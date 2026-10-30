# Application progress + result merge (2026-10-30, proposed)

Merge ListView **Application result** chips into **Application progress**. Last process step stays **At the migration service**; coverage chips sit to its right (not extra process steps).

**Status:** **Done** (2026-10-30). Stop F5, rebuild Blazor host, hard-refresh.

## Locked decisions

| # | Decision | Choice |
|---|----------|--------|
| 1 | Result stack placement | **A** — to the **right** of **At the migration service**, same row as the stepper, chips **stacked vertically** |
| 2 | Separate ListView column | **Remove** Application result column after merge |
| 3 | When to show chips | Once the last hand-off is current or done. Via ministry and Direct Issuance/Change: **At the migration service** / **AS number received**. Direct routes that end at hand-in: **Submitted to migration service**. Only when the profile produces invitation, visa, work permit, or border zone (not registration rejection-only) |
| 4 | Scope | **Via-ministry ListView** and **case Progress tab** |
| 5 | Chip look | Same labels/colors as today (`Invitation x/y`, `Rejection n`, `Visa x/y`), stacked |

## Not in this change

- Do not add process steps after **At the migration service**.
- Process number stays on the migration step / Process number column (not a new step).
- Application result **workspace tab** (compose issued docs) is unchanged — this is ListView + Progress timeline chrome only.

## Screens

| File | State |
|------|--------|
| [application-progress-result-merged-listview-prototype.jpg](./application-progress-result-merged-listview-prototype.jpg) | Via-ministry ListView: no result column; vertical chips after last step when at migration |
| [application-progress-result-merged-progress-tab-prototype.jpg](./application-progress-result-merged-progress-tab-prototype.jpg) | Progress tab: same stack to the right of **At the migration service** |

## Example ListView rows

| Application | Progress | Chips in progress cell |
|-------------|----------|------------------------|
| Completed (Issued) | … → At the migration service (done) | Invitation / Rejection / Visa stacked |
| Mid-flight (ministry) | Current before migration | No chips yet |

## Related

- Prior ListView stepper: [application-profile-instance-listview-progress-README.md](./application-profile-instance-listview-progress-README.md)
- Result coverage chips: [application-profile-instance-listview-result-coverage-README.md](./application-profile-instance-listview-result-coverage-README.md)