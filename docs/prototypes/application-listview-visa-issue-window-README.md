# Application ListView — visa issue window (proposed)

Officers on **Wiza we Iş Rugsatnamasyny Uzaltmak** issue a work permit for every expected person, then create the new visas. The visas are due within a set number of days after the work-permit letter date. One counter sits on the application row, on the same line as the **Wiza** pill, to its right.

**Status:** Implemented. The HTML remains the visual reference.

**Sample “today” in the mock:** 16.11.2026. Limit **45** days. Warning from **5** days left, and on day **0**.

## Locked in this revision

| Decision | Choice |
|----------|--------|
| Profiles | **Wiza we Iş Rugsatnamasyny Uzaltmak** only |
| Clock starts | The one work-permit letter already includes every linked person. Counter starts on that letter’s `IssuedDate` and shows **45** that day |
| Anchor | `WorkPermit.IssuedDate` |
| While visas are short and more than 5 days remain | Teal on the counter only (`#042f2e`). **Wiza** pill stays gray |
| Last 5 days, visas still missing, counter above 0 | Amber on the counter and on the **Wiza** pill. Row stays CO gold |
| Day 0, visas still missing | Row turns green and moves to the **Resmileşdirildi** tab. Counter and **Wiza** pill stay amber at **0**. Officers can still create visas. The stored step stays **CO belgisi alyndy** |
| All visas issued in time | Row green. Success teal on the counter (`#0f766e`). Number is the days still left on the day the last visa was issued (letter 01.11.2026, last visa 10.11.2026 → **36 gün**). **Wiza** pill stays the usual issued green |
| Last visa after day 0 | Row green. Success teal on the counter, number **0**. **Wiza** pill stays the usual issued green |
| Row background | CO gold until this profile is finished: every work permit is in, and either every visa is in or the counter is **0**. Other progress colors are unchanged. Report Dashboard is unchanged |
| Other profiles | No counter |

## Counting

```text
deadline = WorkPermit.IssuedDate + VisaIssueWindowDays
days left = max(0, deadline − today)
```

On the letter’s own day the counter shows **45**. On the deadline day it shows **0**.

When the last visa is issued, the counter stops on the days-left figure of that day. Example in the mock: letter 01.11.2026, last visa 10.11.2026, deadline 16.12.2026, frozen **36 gün**.

## Screen

| File | State |
|------|--------|
| [application-listview-visa-issue-window-preview.html](./application-listview-visa-issue-window-preview.html) | Gold list + counter + configuration card |

Image OCR may garble Türkmençe; this table is the source of truth.
