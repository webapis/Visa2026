# Portal edit-page capture (Registration)

**Live capture:** 2026-11-10T12:09:11.520Z via Chrome DevTools Console `copy(JSON.stringify(...))`  
**URL:** https://rgstr.migration.gov.tm/panel/applications/edit/11276/

| File | Contents |
|------|----------|
| `edit-page-dropdown-catalog-live.json` | Live Console dump (source of truth) |
| `edit-page-dropdown-catalog.json` | Same catalogs (synced from live) |
| `application-types.json` | `#id_type` (7) |
| `location-types.json` | `#id_body_type` (9) |
| `places-of-stay.json` | `#id_body` (1507, with `dataType`) |
| `address-districts.json` | `#id_city` (99, with welayat `group`) |
| `places-of-stay-calik-subset.md` | 19 Çalık-related places |
| `edit-11276-from-user-dump.html` | Earlier full-page HTML dump (CSRF redacted) |

Selectors: [../playwright-rpa-portal-css-selectors.md](../playwright-rpa-portal-css-selectors.md)

**Safety:** discovery only — do not Save/Search against live portal without consent.
