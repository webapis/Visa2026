# Application result overview (2026-09-19, proposed)

Officers need to see at a glance what this case **issued**, what is **missing**, and **how many**, without opening each compose tile. This is a **Result-tab overview**, not a return of the full issuance UI to Ýüztutma (Overview).

**Status:** **Done** (2026-09-19) — Result tab Netije syny + Hereketler. Live UI: completeness ring, tinted totals, per-type progress.

## Suggestion

| Decision | Choice |
|----------|--------|
| Where | **Ýüztutmanyň netijesi** tab only. Do not put compose tiles back on Overview. |
| Expected | Roster size for invitation / work permit / issued visa / border zone. **Issued** counts distinct people on InvitationItem / WorkPermitItem / Visa (one per person). |
| Optional | **Rejection** is not required. Card shows RejectionItem people. Zero is not missing and does not enter totals. |
| Totals | Do **not** add people across types (14 WP + 11 visa ≠ 25 issued). Header badge = required **types** complete / short. Each type card shows its own ratio and **percentage**. Coverage is distinct **roster** people only. |
| Actions | Existing tiles stay **below** the overview (open / + Add). Overview cards are status, not compose. |
| Overview tab later | Optional one-line teaser on Ýüztutma (“1 / 14 iş rugsady · 0 / 14 wiza”) — not in this prototype. |

Example (14 people, one work permit covering the roster, 11 visas, optional rejection unused): Iş rugsady **14 / 14 · 100%**, Ret **0** optional, Berlen wiza **11 / 14 · 79%**. No combined 26 / 28 people total.

## Screen

| File | State |
|------|--------|
| [application-profile-instance-result-overview-prototype.png](./application-profile-instance-result-overview-prototype.png) | Result tab: Netije syny totals + per-type cards, then Hereketler tiles |

Image labels may be slightly garbled; this table is the source of truth.