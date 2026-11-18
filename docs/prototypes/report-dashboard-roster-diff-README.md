# Report Dashboard — roster change log (prototype)

Interactive HTML mock (open in a browser):

[report-dashboard-roster-diff.html](./report-dashboard-roster-diff.html)

## What it shows

| Piece | Intent |
|-------|--------|
| Total `138` + `↓ 4 vs last Excel` | Officer sees the count moved |
| Amber banner | Summary of added/removed vs the previous Excel snapshot |
| **View changes** dialog | Lists people who left / entered so the total change is explained |
| **Open selected in Excel** | Reminder that Excel is when the next snapshot is written |

## Locked for v1 (from product answers)

| Decision | Choice |
|----------|--------|
| Snapshot source | Only on Open in Excel / Open selected in Excel |
| Reports | aktiv personeller, aktiv aile üyeleri, çalışma izni listesi |
| Diff depth | Membership (PersonId) only — not field-level edits |
| Goal | Explain who caused the report total to change |

Sample names and counts are layout only.