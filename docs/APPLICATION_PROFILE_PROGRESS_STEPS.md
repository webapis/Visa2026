# Application Profile — progress step visibility

**Purpose:** Reference for which progress-timeline steps each Calik Energi Application Profile shows.
**Source of truth (seed):** `Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/application-profile.calik-energi.json` → each row's `ProgressSteps` array (`Key` + `Visible`).
**Runtime:** `ApplicationProfileProgressStepVisibility` and the case workspace timeline (`ApplicationWorkspaceProgressTimeline`).
**Related:** [`APPLICATION_PROFILE_PLAN.md`](APPLICATION_PROFILE_PLAN.md) · skill visa2026-application-profile · progress transitions visa2026-application-progress.

**Snapshot:** 35 profiles in the Calik Energi tenant catalog (synced from Excel). Re-check the JSON if profiles are added or `ProgressSteps` change.
**Excel export:** [`APPLICATION_PROFILE_PROGRESS_STEPS.xlsx`](APPLICATION_PROFILE_PROGRESS_STEPS.xlsx) — sheets: Column legend, Visibility patterns, Full matrix, Notes (`Y`/`N` for visible).

Legend in matrices: **✓** = `Visible: true`, **—** = `Visible: false`.

---

## Step columns (full names)

| Abbr | JSON key | Full name | What it controls |
|------|----------|-----------|------------------|
| **O** | `office` | Office preparation | First officer step (prepare the case in office). |
| **Min** | `ministry` | Ministry legs | Ministry approval steps. Shown on via-ministry routes; hidden on direct routes. |
| **Sub** | `migration-submitted` | Submitted (migration / organization) | Hand-in to migration service or organization (Seretmezlik). |
| **Mig** | `migration` | Migration / AS number | AS (CO) process number step. |
| **Iss** | `process-issued` | Issued (completion) | Final success / issued result step. |
| **Rej** | `process-rejected` | Rejected | Rejected outcome step. |
| **Can** | `process-cancelled` | Cancelled | Cancelled outcome step. |

---

## Visibility patterns

| Pattern | O | Min | Sub | Mig | Iss | Rej | Can | Count | Profiles |
|---------|:-:|:---:|:---:|:---:|:---:|:---:|:---:|------:|----------|
| Seretmezlik (Submitted only; Office off) | — | — | ✓ | — | — | — | — | 1 | `seretmezlik` |
| Registration (Office + Issued) | ✓ | — | — | — | ✓ | — | — | 8 | `check_in_from_abroad`, `check_in_internal`, `check_out`, `check_out_internal`, `reg_extension`, `reg_info_change_address`, `reg_info_change_passport`, `reg_info_change_visa` |
| Cancel / business trip + Issued | ✓ | — | ✓ | — | ✓ | — | — | 9 | `business_trip_arrival`, `business_trip_departure`, `cancel_application`, `cancel_borderzone`, `cancel_invitation`, `cancel_invitation_wp`, `cancel_visa`, `cancel_visa_wp`, `cancel_workpermit` |
| Direct change (AS only) | ✓ | — | ✓ | ✓ | — | — | — | 3 | `change_invitation`, `pasport_change`, `visa_category_change` |
| Via-ministry issuance | ✓ | ✓ | ✓ | ✓ | — | — | — | 14 | `change_workpermit`, `extend_visa_wp`, `get_border_zone`, `get_invitation`, `get_invitation_according_to_wp`, `get_invitation_fm`, `get_invitation_service_passport`, `get_invitation_wp`, `visa_exit`, `visa_ext`, `visa_ext_according_to_wp`, `visa_ext_fm`, `visa_for_new_born_fm`, `workpermit_extension` |

---

## Full matrix

| Code | Name | Route | Family | O | Min | Sub | Mig | Iss | Rej | Can |
|------|------|-------|--------|:-:|:---:|:---:|:---:|:---:|:---:|:---:|
| business_trip_arrival | Iş Saparyna Gelmek | DirectToMigrationService | BusinessTrip | ✓ | — | ✓ | — | ✓ | — | — |
| business_trip_departure | Iş Saparyna Gitmek | DirectToMigrationService | BusinessTrip | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_application | Ýüztutmany Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_borderzone | Serhet Ýaka Rugsady Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_invitation | Çakylygy Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_invitation_wp | Çakylyk we Iş Rugsatnamasyny Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_visa | Wizany Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_visa_wp | Wiza we Iş Rugsatnamany Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_workpermit | Iş Rugsatnamany Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| seretmezlik | Seretmezlik | DirectToMigrationService | Cancellation | — | — | ✓ | — | — | — | — |
| change_invitation | Çakylygy üýtgetmek | DirectToMigrationService | Change | ✓ | — | ✓ | ✓ | — | — | — |
| pasport_change | Wizany KP>Täze Pasporta Geçirmek | DirectToMigrationService | Change | ✓ | — | ✓ | ✓ | — | — | — |
| visa_category_change | Wiza Kategoriýasyny üýtgetmek | DirectToMigrationService | Change | ✓ | — | ✓ | ✓ | — | — | — |
| change_workpermit | Iş Rugsatnama goşmaça  barjak ýeri | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| extend_visa_wp | Wiza we Iş Rugsatnamasyny Uzaltmak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_border_zone | Serhet Ýaka Üçin Rugsatnama Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_invitation | Çakylyk Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_invitation_according_to_wp | İş Rugsatnama görä Çakylyk Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_invitation_fm | Çakylyk Almak Maşgala agzasy | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_invitation_service_passport | Gulluk Pasporty Üçin Çakylyk Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_invitation_wp | Çakylyk we Iş Rugsatnamasyny Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_exit | Çykyş  Wiza Resmileşdirmek | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_ext | Wiza Möhletini Uzaltmak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_ext_according_to_wp | Iş Rugsatnamasyna Görä Wizany Uzaltmak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_ext_fm | Wiza Möhletini Uzaltmak Maşgala agzasy | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_for_new_born_fm | Täze dogulan çaga wiza resmileşdirmek Maşgala agzasy | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| workpermit_extension | Iş Rugsatnamasyny Uzaltmak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| check_in_from_abroad | Hasaba Almak (Daşary ýurtdan gelmegi sebäpli) | DirectToMigrationService | Registration | ✓ | — | — | — | ✓ | — | — |
| check_in_internal | Hasaba Almak (Welaýatdan gelmegi sebäpli) | DirectToMigrationService | Registration | ✓ | — | — | — | ✓ | — | — |
| check_out | Hasapdan Çykarmak (Daşary ýurda gitmegi) | DirectToMigrationService | Registration | ✓ | — | — | — | ✓ | — | — |
| check_out_internal | Hasapdan Çykarmak (Başga welaýata gitmegi) | DirectToMigrationService | Registration | ✓ | — | — | — | ✓ | — | — |
| reg_extension | Hasaba alyşy uzaltmak | DirectToMigrationService | Registration | ✓ | — | — | — | ✓ | — | — |
| reg_info_change_address | Hasaba alyş maglumatyň üýtgemegi (Salgy Çalyşmagy) | DirectToMigrationService | Registration | ✓ | — | — | — | ✓ | — | — |
| reg_info_change_passport | Hasaba alyş maglumatyň üýtgemegi (Pasport Çalışmagy) | DirectToMigrationService | Registration | ✓ | — | — | — | ✓ | — | — |
| reg_info_change_visa | Hasaba alyş maglumatyň üýtgemegi (Visa Çalyşmagy) | DirectToMigrationService | Registration | ✓ | — | — | — | ✓ | — | — |

---

## Notes

- Via-ministry **Issuance** profiles hide **Issued / Rejected / Cancelled**; the route completes with the AS (CO) number on **Mig**.
- **Change** family (`change_invitation`, `visa_category_change`, `pasport_change`): Submitted + Migration/AS only (Iss/Rej/Can off).
- **Cancellation** (except Seretmezlik) and **business trip** profiles: Office + Submitted + Issued.
- **Registration** profiles: Office + Issued (Submitted/Migration off).
- **Seretmezlik**: Office off; Submitted only (organization hand-in).
- To refresh after seed edits, re-read `ProgressSteps` from the JSON (or sync from the Excel workbook).
