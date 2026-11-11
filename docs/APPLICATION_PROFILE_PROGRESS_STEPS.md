# Application Profile — progress step visibility

**Purpose:** Reference for which progress-timeline steps each Calik Energi Application Profile shows.  
**Source of truth (seed):** `Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/application-profile.calik-energi.json` → each row’s `ProgressSteps` array (`Key` + `Visible`).  
**Runtime:** `ApplicationProfileProgressStepVisibility` (`Visa2026.Module/BusinessObjects/ApplicationProfileProgressStepVisibility.cs`) and the case workspace timeline (`ApplicationWorkspaceProgressTimeline`).  
**Related:** [`APPLICATION_PROFILE_PLAN.md`](APPLICATION_PROFILE_PLAN.md) · skill [visa2026-application-profile](../.cursor/skills/visa2026-application-profile/SKILL.md) · progress transitions [visa2026-application-progress](../.cursor/skills/visa2026-application-progress/SKILL.md).

**Snapshot:** 35 profiles in the Calik Energi tenant catalog (as of this document). Re-check the JSON if profiles are added or `ProgressSteps` change.  
**Excel export:** [`APPLICATION_PROFILE_PROGRESS_STEPS.xlsx`](APPLICATION_PROFILE_PROGRESS_STEPS.xlsx) — sheets: Column legend, Visibility patterns, Full matrix, Notes (`Y`/`N` for visible).

Legend in matrices: **✓** = `Visible: true`, **—** = `Visible: false`.

---

## Step columns (full names)

| Abbr | JSON key | Full name | What it controls |
|------|----------|-----------|------------------|
| **O** | `office` | Office preparation | First officer step (prepare the case in office). Present on every profile today. |
| **Min** | `ministry` | Ministry legs | Ministry approval steps (e.g. Türkmenenergo, Energetika). Shown on via-ministry routes; hidden on direct routes. |
| **Sub** | `migration-submitted` | Submitted (migration / organization) | Hand-in step: submitted to migration service, or to organization for Seretmezlik-style profiles (`RequireSubmitToOrganization`). |
| **Mig** | `migration` | Migration / AS number | Migration-service step where the AS (CO) process number is recorded and the case waits on that number. |
| **Iss** | `process-issued` | Issued (completion) | Final success / issued result step (e.g. Işlendi / Resmileşdirildi). |
| **Rej** | `process-rejected` | Rejected | Rejected outcome step on the progress timeline. |
| **Can** | `process-cancelled` | Cancelled | Cancelled outcome step on the progress timeline. |

Constant names in code (`ApplicationProfileProgressStepVisibility`): `Office`, `Ministry`, `Submission`, `AsNumber`, `Issued`, `Rejected`, `Cancelled`.

---

## Visibility patterns

| Pattern | O | Min | Sub | Mig | Iss | Rej | Can | Count | Typical use |
|---------|:-:|:---:|:---:|:---:|:---:|:---:|:---:|------:|-------------|
| Via-ministry issuance | ✓ | ✓ | ✓ | ✓ | — | — | — | 14 | Invitation / visa / WP / border-zone issuance and extensions. Route ends at AS number; Issued/Rejected/Cancelled chips stay off. |
| Short direct (hand-in only) | ✓ | — | ✓ | — | — | — | — | 13 | Seretmezlik, registration, business trip, cancel application / border zone. Ends at Submitted (organization or migration hand-in). |
| Cancel + Issued | ✓ | — | ✓ | — | ✓ | — | — | 5 | Cancel invitation / visa / work permit (and WP variants). Keeps Issued so officers can mark completion after hand-in. |
| Direct change (AS only) | ✓ | — | ✓ | ✓ | — | — | — | 3 | `change_invitation`, `visa_category_change`, `pasport_change` — Submitted + Migration/AS; Issued/Rejected/Cancelled hidden. |

---

## Full matrix

| Code | Name | Route | Family | O | Min | Sub | Mig | Iss | Rej | Can |
|------|------|-------|--------|:-:|:---:|:---:|:---:|:---:|:---:|:---:|
| get_invitation_wp | Çakylyk we Iş Rugsatnamasyny Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_invitation_fm | Çakylyk Almak Maşgala agzasy | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_invitation | Çakylyk Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_invitation_service_passport | Gulluk Pasporty Üçin Çakylyk Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_invitation_according_to_wp | İş Rugsatnama görä Çakylyk Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_ext | Wiza Möhletini Uzaltmak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_ext_fm | Wiza Möhletini Uzaltmak Maşgala agzasy | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_ext_according_to_wp | Iş Rugsatnamasyna Görä Wizany Uzaltmak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_exit | Çykyş Wiza Resmileşdirmek | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| visa_for_new_born_fm | Täze dogulan çaga wiza resmileşdirmek Maşgala agzasy | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| extend_visa_wp | Wiza we Iş Rugsatnamasyny Uzaltmak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| workpermit_extension | Iş Rugsatnamasyny Uzaltmak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| get_border_zone | Serhet Ýaka Üçin Rugsatnama Almak | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| change_workpermit | Iş Rugsatnama goşmaça barjak ýeri | ViaMinistries | Issuance | ✓ | ✓ | ✓ | ✓ | — | — | — |
| change_invitation | Çakylygy üýtgetmek | DirectToMigrationService | Change | ✓ | — | ✓ | ✓ | — | — | — |
| visa_category_change | Wiza Kategoriýasyny üýtgetmek | DirectToMigrationService | Change | ✓ | — | ✓ | ✓ | — | — | — |
| pasport_change | Wizany KP>Täze Pasporta Geçirmek | DirectToMigrationService | Change | ✓ | — | ✓ | ✓ | — | — | — |
| cancel_invitation | Çakylygy Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_invitation_wp | Çakylyk we Iş Rugsatnamasyny Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_visa | Wizany Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_visa_wp | Wiza we Iş Rugsatnamany Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| cancel_workpermit | Iş Rugsatnamany Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | ✓ | — | — |
| seretmezlik | Seretmezlik | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | — | — | — |
| cancel_application | Ýüztutmany Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | — | — | — |
| cancel_borderzone | Serhet Ýaka Rugsady Ýatyrmak | DirectToMigrationService | Cancellation | ✓ | — | ✓ | — | — | — | — |
| check_in_from_abroad | Hasaba Almak (Daşary ýurtdan gelmegi sebäpli) | DirectToMigrationService | Registration | ✓ | — | ✓ | — | — | — | — |
| check_in_internal | Hasaba Almak (Welaýatdan gelmegi sebäpli) | DirectToMigrationService | Registration | ✓ | — | ✓ | — | — | — | — |
| check_out | Hasapdan Çykarmak (Daşary ýurda gitmegi) | DirectToMigrationService | Registration | ✓ | — | ✓ | — | — | — | — |
| check_out_internal | Hasapdan Çykarmak (Başga welaýata gitmegi) | DirectToMigrationService | Registration | ✓ | — | ✓ | — | — | — | — |
| reg_extension | Hasaba alyşy uzaltmak | DirectToMigrationService | Registration | ✓ | — | ✓ | — | — | — | — |
| reg_info_change_address | Hasaba alyş maglumatyň üýtgemegi (Salgy Çalyşmagy) | DirectToMigrationService | Registration | ✓ | — | ✓ | — | — | — | — |
| reg_info_change_passport | Hasaba alyş maglumatyň üýtgemegi (Pasport Çalışmagy) | DirectToMigrationService | Registration | ✓ | — | ✓ | — | — | — | — |
| reg_info_change_visa | Hasaba alyş maglumatyň üýtgemegi (Visa Çalyşmagy) | DirectToMigrationService | Registration | ✓ | — | ✓ | — | — | — | — |
| business_trip_arrival | Iş Saparyna Gelmek | DirectToMigrationService | BusinessTrip | ✓ | — | ✓ | — | — | — | — |
| business_trip_departure | Iş Saparyna Gitmek | DirectToMigrationService | BusinessTrip | ✓ | — | ✓ | — | — | — | — |

---

## Notes

- Via-ministry **Issuance** profiles hide **Issued / Rejected / Cancelled** as progress result steps; the route completes with the AS (CO) number on **Mig**.
- **Change** family (`change_invitation`, `visa_category_change`, `pasport_change`): Submitted + Migration/AS only (Iss/Rej/Can off).
- **Seretmezlik** and other short directs end at **Sub** (organization / migration hand-in). List-row green for a finished short route is display remapping (`SUBMITTED_MIGRATION` → Issued appearance), not an enabled **Iss** step.
- Cancel-with-**Iss** profiles keep Issued so officers can mark completion after hand-in.
- Retired extension-cancellation types (`cancel_visa_ext`, `cancel_visa_wp_ext`) are not in the active picker; Seretmezlik covers that family. Existing instances may still open.
- To refresh this matrix after seed edits, re-read `ProgressSteps` from `application-profile.calik-energi.json` (do not invent visibility outside that catalog).