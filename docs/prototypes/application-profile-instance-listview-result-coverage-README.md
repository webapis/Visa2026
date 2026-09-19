# Application instance ListView — result coverage (2026-09-19, proposed)

Officers already see per-type **people coverage** on **Ýüztutmanyň netijesi** (Işlenen / Garaşylýan). This prototype puts the **same ratios** on the Application Profile Instance ListView so they do not have to open every case.

**Status:** **Done** (2026-09-19) — ListView last column. Stop F5, rebuild Blazor host, hard-refresh.

## Locked for this set (proposed)

| Decision | Choice |
|----------|--------|
| Where | Native instance lists: `Application_ListView`, `Application_ListView_ViaMinistries`, `Application_ListView_DirectMigration` |
| Column | **One** last column: **Ýüztutmanyň netijesi** |
| Value | Same as Netije syny: distinct **roster people** with that type / roster size |
| Hidden types | Types this profile does not produce are omitted (not `0 / N`) |
| Rejection | Optional: show people count only (`0` is gray, not missing) |
| Empty profile | Registration / no produce → `—` |
| Click | Chips are display-only. Row still opens the case workspace |
| Not in this column | Header letter counts (those stay on Hereketler) |

## Example rows

| Belgisi | Adam | Ýüztutmanyň netijesi |
|---------|------|----------------------|
| 2026-01555 | 14 | Iş rugsady **14 / 14** · Ret **0** · Wiza **11 / 14** |
| 2026-01472 | 2 | Çakylyk **2 / 2** · Iş rugsady **0 / 2** · Ret **0** · Wiza **2 / 2** |
| 2026-01381 | 6 | Çakylyk **0 / 6** |
| 2026-01290 | 3 | — (profil netije bermeýär) |
| 2026-01211 | 8 | Çakylyk **8 / 8** · Iş rugsady **8 / 8** · Wiza **8 / 8** |
| 2026-01104 | 1 | Serhet **0 / 1** · Wiza **0 / 1** |

Chip colors: green = Işlenen, red = Ýetmezçilik, gray = Ret opsiýonal.

## Screen

| File | State |
|------|--------|
| [application-profile-instance-listview-result-coverage-prototype.png](./application-profile-instance-listview-result-coverage-prototype.png) | Via-ministry ListView + Netije chips |
| [application-profile-instance-listview-result-coverage-preview.html](./application-profile-instance-listview-result-coverage-preview.html) | Same mock, open in a browser |

Image OCR may garble Türkmençe; this table is the source of truth.