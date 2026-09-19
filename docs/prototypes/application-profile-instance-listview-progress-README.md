# Application instance ListView - progress stepper (2026-09-19, proposed)

Officers already see the full path on the case workspace **Ýüztutma ýagdaýy** stepper (Ofisde taýýarlyk -> ministry legs -> Migrasiýa). This prototype puts the **same steps** on the Application Profile Instance ListView so they do not have to open every case - same idea as **Ýüztutmanyň netijesi** chips.

**Status:** **Done** (2026-09-19). Stop F5, rebuild Blazor host, hard-refresh.

## Locked for this set (proposed)

| Decision | Choice |
|----------|--------|
| Where | Native instance lists: `Application_ListView`, `Application_ListView_ViaMinistries`, `Application_ListView_DirectMigration` |
| Column | **One** column: **Ýüztutmanyň ýagdaýy** (replaces the single Ýagdaý text, not a second progress column) |
| Visual | Compact copy of the workspace stepper, not chips |
| Via-ministry | Ofisde taýýarlyk + each approval-leg ministry + Migrasiýa |
| Direct migration | Ofisde taýýarlyk + Migrasiýa only |
| Done | Green check + step date |
| Current | Blue ring + status (Ylalaşylýar / Dowam edýär / Işlenýär) |
| Pending | Gray numbered circle, no date |
| Rejected | Red X + Ret edildi |
| Click | Display-only. Row still opens the case workspace |
| Placement | Immediately before **Ýüztutmanyň netijesi** (last two columns). No row-state fill on either cell |
| Hidden | **Tassyklama möhleti** and **Migrasiýa möhleti** — stepper replaced them |

## Example rows

| Belgisi | Route | Ýüztutmanyň ýagdaýy |
|---------|-------|---------------------|
| 2026-01555 | Via | Ofis done · Türkmenenergo done · Energetika done · Migrasiýa done (Işlendi) |
| 2026-01472 | Via | Ofis done · Türkmenenergo done · **Energetika current** · Migrasiýa pending |
| 2026-01381 | Via | Ofis done · **Türkmenenergo current** · Energetika · Migrasiýa |
| 2026-01290 | Direct | **Ofis current** · Migrasiýa |
| 2026-01211 | Via | Ofis done · Türkmenenergo done · **Energetika rejected** · Migrasiýa |

## Screen

| File | State |
|------|--------|
| [application-profile-instance-listview-progress-prototype.png](./application-profile-instance-listview-progress-prototype.png) | Via-ministry ListView + mini stepper |
| [application-profile-instance-listview-progress-preview.html](./application-profile-instance-listview-progress-preview.html) | Same mock, open in a browser |

Image OCR may garble Turkmen; this table is the source of truth.