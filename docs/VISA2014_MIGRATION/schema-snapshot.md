# VISA2014 schema snapshot (bootstrap only)

**Global table index** from one-time bootstrap. Per-BO columns, FKs, and mapping detail live in **`discovery/{Entity}.yaml`** — not here.

**Status:** complete (2026-06-20) — `order.yaml` → `bootstrapOnce` → `global-schema-index`.

**Source:** `localhost\SQLEXPRESS` / **`VISA2015`** via sqlcmd (Windows auth).

**Workflow:** [discovery/README.md](./discovery/README.md)

---

## Database confirmation

| Check | Value |
|-------|-------|
| Legacy database name | `VISA2015` |
| SQL instance (local) | `localhost\SQLEXPRESS` |
| Bootstrap date | 2026-06-20 |
| Total `dbo` tables | 94 |
| Active `Person` rows (`GCRecord IS NULL`) | 2,569 |
| Active `Passport` rows | ~5,768 total rows in table; all active persons have ≥1 passport |

---

## Notable patterns

| Pattern | Notes |
|---------|--------|
| **XPO / XAF** | `Oid` PK (uniqueidentifier); soft delete **`GCRecord IS NULL`** = active |
| **Audit / system (skip import)** | `AuditDataItemPersistent`, `XPWeakReference`, `AuditedObjectWeakReference`, `ModuleInfo`, `SecuritySystem*`, `XPObjectType`, `XPUserSettings*` |
| **Person model** | `dbo.Person` master; `dbo.Employee` 1:1 extension on same `Oid`; `dbo.Passport` child (PassportNumber, PersonalNumber on passport) |
| **Lookup tables** | `Country` uses `NameOfCountryL` (ISO-style, e.g. TUR); `Gender` uses `TypeOfGenderL` (Ayal/Erkek); **`MaritalStatus` is not a simple catalog** — `Status` holds long free-text family descriptions |
| **Views / computed** | `I*_Data` tables (e.g. `IRemainingDays_Data`) — treat as read-only / skip for import |
| **Attachments** | `FileData`, `PassportCopy`, `Copy` — attachment wave (last) |

---

## Global table index

Approximate row counts (`sys.partitions`). **Skip?** = default for import dossiers unless a BO maps the table.

| Schema | Table | ~Rows | Skip? | Notes |
|--------|-------|-------|-------|-------|
| dbo | AuditDataItemPersistent | 2447042 | yes | XAF audit |
| dbo | XPWeakReference | 1302434 | yes | XPO infrastructure |
| dbo | AuditedObjectWeakReference | 204580 | yes | XAF audit |
| dbo | PersonInApplication | 34161 | no | Application domain |
| dbo | IRemainingDays_Data | 28337 | yes | Computed/view data |
| dbo | IMessage_Data | 19897 | yes | Computed/view data |
| dbo | PassportCopy | 15493 | defer | Attachments |
| dbo | Application | 11952 | no | Application header |
| dbo | IRegistration_Data | 11952 | yes | Computed/view data |
| dbo | PersonInInvitation | 11676 | no | Invitation domain |
| dbo | Visa | 8867 | no | Visa records |
| dbo | AddressOfResidence | 8385 | defer | Person child |
| dbo | SimpleProcessApplication | 8162 | no | Application subtype |
| dbo | Passport | 5768 | no | Person child — separate BO import |
| dbo | WorkPermitLocation | 5691 | no | Work permit |
| dbo | WorkPermit | 5317 | no | Work permit header |
| dbo | **Person** | 5262 | no | **Person master (2,569 active)** |
| dbo | Education | 5080 | defer | Person child |
| dbo | TravelInformation | 5018 | defer | Travel history |
| dbo | WorkHistoryOfEmployee | 4970 | defer | Employee history |
| dbo | **Employee** | 4852 | no | 1:1 Person extension |
| dbo | LongProcessApplication | 3790 | no | Application subtype |
| dbo | BorderZoneForVisa | 3421 | no | Border zone |
| dbo | ApplicationResult | 2939 | no | Application |
| dbo | MaritalStatus | 2369 | lookup | Legacy shape ≠ Visa2026 catalog — see Person dossier |
| dbo | Country | 1861 | lookup | Map `NameOfCountryL` → Visa2026 `Code` |
| dbo | Address | 1392 | defer | |
| dbo | EducationInstitution | 1230 | lookup | |
| dbo | FamilyProofDocument | 994 | defer | |
| dbo | Speciality | 741 | lookup | |
| dbo | Position | 689 | lookup | |
| dbo | Tasaron | 638 | lookup | Subcontractor candidate |
| dbo | FamilyMember | 410 | review | May overlap Person discriminator |
| dbo | WorkPermitLetter | 362 | defer | |
| dbo | AnketaMaksat | 323 | lookup | |
| dbo | ApplicationType | 231 | lookup | Layer 3 required |
| dbo | XPUserSettingsAspect | 177 | yes | XPO |
| dbo | SecuritySystemTypePermissionsObject | 172 | yes | Security |
| dbo | AddressOnBusinessTrip | 146 | defer | |
| dbo | Salary | 144 | defer | |
| dbo | ApplicationTypeForEmployee | 140 | lookup | |
| dbo | CheckPoint | 113 | lookup | |
| dbo | Copy | 107 | defer | Files |
| dbo | FileData | 107 | defer | Attachments |
| dbo | PurposeOfTravel | 96 | lookup | |
| dbo | ApplicationTypeForFamilyMember | 91 | lookup | |
| dbo | XPObjectType | 88 | yes | XPO |
| dbo | ReportData | 86 | yes | Reports |
| dbo | Contract | 82 | lookup | → ProjectContract |
| dbo | PrefferedVisaCategory | 72 | lookup | |
| dbo | Relation | 54 | lookup | → Relationship |
| dbo | Region | 50 | lookup | |
| dbo | Department | 49 | lookup | |
| dbo | DepartmentForRegistration | 44 | lookup | |
| dbo | BaseApplicationType | 42 | lookup | |
| dbo | VisaIssuedPlace | 37 | lookup | |
| dbo | EducationLevel | 24 | lookup | |
| dbo | PassportType | 24 | lookup | |
| dbo | ModuleInfo | 19 | yes | XAF |
| dbo | SecuritySystemUserUsers_SecuritySystemRoleRoles | 19 | yes | Security |
| dbo | Bellik | 18 | lookup | |
| dbo | DocumentOfAddress | 18 | defer | |
| dbo | SecuritySystemUser | 18 | yes | Security |
| dbo | XPUserSettings | 18 | yes | XPO |
| dbo | AppliedMinistery | 16 | lookup | |
| dbo | Gender | 16 | lookup | Map `TypeOfGenderL` |
| dbo | IsInvitationWithWorkPermit | 16 | lookup | |
| dbo | IsWizaWithWorkPermit | 16 | lookup | |
| dbo | VisaPeriod | 15 | lookup | |
| dbo | Company | 13 | org | |
| dbo | GosmacaMaglumatYeri | 11 | lookup | |
| dbo | CompanyRepresentative | 10 | org | |
| dbo | IPersonn_SpidKepilnama | 9 | defer | |
| dbo | AppConf | 8 | lookup | |
| dbo | IPluralFamilyMember | 8 | yes | |
| dbo | Plural | 8 | yes | |
| dbo | IVisaType_Data | 6 | yes | |
| dbo | VisaType | 6 | lookup | |
| dbo | SecuritySystemRole | 5 | yes | Security |
| dbo | VisaCategory | 4 | lookup | |
| dbo | IslemgeRugsatEdilenYer | 3 | lookup | |
| dbo | Urgency | 3 | lookup | |
| dbo | IdentitiDocCopied | 1 | defer | |
| dbo | SecuritySystemMemberPermissionsObject | 1 | yes | Security |
| dbo | SecuritySystemObjectPermissionsObject | 1 | yes | Security |
| dbo | SecuritySystemRoleParentRoles_SecuritySystemRoleRoles | 1 | yes | Security |
| dbo | AuditTrail | 0 | yes | |
| dbo | IApplication_WorkPermit | 0 | yes | |
| dbo | ILongProcessApplication_YlalasykNusga | 0 | yes | |
| dbo | PassportIssuedPlace | 0 | lookup | |
| dbo | Proje | 0 | lookup | |
| dbo | UserCode | 0 | yes | |
| dbo | Visibility | 0 | yes | |

*(Remaining low-row tables included above; full query returned 94 tables.)*

---

## Discovery log

#### 2026-06-20 — Bootstrap + Person schema probe

- **Tool:** sqlcmd → `VISA2015` (MCP `visa2014-sql-local` not loaded in session)
- **Notes:** `Person.IDNumber` often holds employer/subcontractor text, **not** civil ID — use `Passport.PersonalNumber` for `Person.PersonalNumber`
- **Follow-ups:** Person dossier complete; Passport BO import row in `order.yaml` (future); MaritalStatus mismatch needs confirmation policy
