# Reference: Application Profile

Companion to [SKILL.md](./SKILL.md).

---

## File map

| Area | Path |
|------|------|
| Profile BO + children | `Visa2026.Module/BusinessObjects/ApplicationProfile.cs` |
| Workspace host (mock UI) | `Visa2026.Module/BusinessObjects/ApplicationWorkspace/ApplicationWorkspaceHost.cs` |
| Workspace mock service | `Visa2026.Module/Services/ApplicationWorkspace/ApplicationWorkspaceMockQueryService.cs` |
| Workspace open helper | `ApplicationWorkspaceOpenHelper.cs` |
| Parent Application FK | `Visa2026.Module/BusinessObjects/Application.cs` (`ApplicationProfile`, `ApplyDefaultsForApplicationProfile`) |
| Deprecated Type | `Visa2026.Module/BusinessObjects/LookupBusinessObjects.cs` (`ApplicationType`) |
| EF mapping | `Visa2026.Module/BusinessObjects/Visa2026DbContext.cs` |
| Permissions | `Visa2026.Module/DatabaseUpdate/Updater.cs` |
| Type configuration seed | `DatabaseUpdate/ApplicationTypeConfigurationApplier.cs`, `ApplicationTypeConfigurationCatalog.json` |
| Type → Profile seed | `DatabaseUpdate/ApplicationProfileFromApplicationTypeMapper.cs`, `ApplicationProfileSeedSync.cs`, `ApplicationProfileSeedUpdater.cs` |
| Config resolver | `BusinessObjects/ApplicationProfileConfigurationResolver.cs`, `Application.ConfigurationVisibility.cs` (`Cfg*` for Appearance) |
| Profile seed startup gate | `Visa2026.Blazor.Server/Services/ApplicationProfileSeedGate.cs` |
| Config lock | `ApplicationProfileLockHelper`, `ApplicationProfileDetailViewController`, `ApplicationProfileConfigLockObjectSpaceHooks`, `ApplicationProfileCloneController` |
| Profile schema heal | `DatabaseUpdate/ApplicationProfileSchemaSql.cs` |
| Lock helper | `ApplicationProfileLockHelper` in `ApplicationProfile.cs` |
| Wizard UX | `ApplicationProfileWizard*`, `ApplicationProfileWizardComponent.razor` |
| Profile overview (mock) | `ApplicationProfileOverview*`, `ApplicationProfileOverviewComponent.razor` |
| Profile picker at create | `ApplicationProfilePicker*`, `ApplicationProfilePickerNewController` |
| Officer manual | `user-manual/docs/en/guides/applications/application-profiles.md`, `administration/configuration/application-profiles.md` |
| Registry | `docs/DEPRECATED.md` |
| Plan | `docs/APPLICATION_PROFILE_PLAN.md` |

### Planned (not yet present — create when slice starts)

| Area | Expected path |
|------|----------------|
| **Workspace UI (10a done)** | `ApplicationWorkspaceHost`, `ApplicationWorkspacePropertyEditor`, `ApplicationWorkspaceComponent.razor` |
| Person M2M | `Application` People collection + resolve service |

---

## Field classification (Excel E–H → plan)

| Excel col | Plan term | Storage |
|-----------|-----------|---------|
| G Configuration Related = 1 | Live on profile | `ApplicationProfile` only |
| H Only Per Application = 1 | Persistent on Application | `Application` (+ defaults on profile) |
| E Visibility on Application = 1 | Show on Application UI | Appearance / dynamic form |
| F Editable Per Application = 1 | Officer may change | Application field |

### Configuration-related (live) — on `ApplicationProfile`

Identity (Name, Description, Code, SelectionCode) · Route (`ProgressRoute`) · Audience (`ForEmployee`, `ForFamilyMember`, `ForTemporaryVisitor`) · `ActionFamily` · Produce/Cancel booleans · `ApprovalLegs` · SLA days · `NestedTemplates` · `RequirePerson*` toggles · `ApplicabilityCriteria` · `Require*` + `Default*` for catalog fields

### Per-Application (persistent) — on `Application`

Visa Type, Category, Period, Border Zone, Migration Service, Start/End dates, Region, Business trip address, Project, Urgency, Work permit location, Entry date, Entry check point, Authorized signatory, Visa representative

Merge rule: Application value if set; else profile default (plan §4).

---

## `ApplicationProfile` person-config toggles (v1 BO)

| Property | Person / child BO |
|----------|-------------------|
| `RequirePersonPassport` | Passport |
| `RequirePersonEducation` | Education |
| `RequirePersonPosition` | EmployeePositionHistory |
| `RequirePersonAddressOfResidence` | AddressOfResidence |
| `RequirePersonVisa` | Visa |
| `RequirePersonInvitationItem` | InvitationItem |
| `RequirePersonWorkPermitItem` | WorkPermitItem |
| `RequirePersonBorderZoneItem` | BorderZoneItem |
| `RequirePersonSalary` | EmployeeSalary |
| `RequirePersonMedical` | MedicalRecord |
| `RequirePersonRejectionItem` | RejectionItem |
| `RequirePersonTravelHistory` | TravelHistory (M2M — not profile scalar) |

---

## Config lock state A

`ApplicationProfileLockHelper.IsApplicationAtOrPastLockStateA`:

- **Not locked:** `OFFICE_PREPARATION`, `DRAFT`, or no progress code
- **Locked:** any other `LatestPrimaryStateCode` / latest progress state code

`ApplicationProfile.IsConfigLocked` — any linked Application locked.

---

## Dual-read rules (current phase)

| Read path | Rule |
|-----------|------|
| New code | Prefer `Application.ApplicationProfile` |
| Legacy / import | `ApplicationType` FK may still be required on save |
| Appearance | `Application.Cfg*` → `ApplicationProfileConfigurationResolver` (slice 6 done) |
| Progress route | `ApplicationProfileConfigurationResolver` / `ApplicationProgressRouteHelper` (profile-first) |
| Do not | Add new `Show*` on ApplicationType |

---

## Prototype → implementation mapping

| Prototype | Slice | Deliverable |
|-----------|-------|-------------|
| `application-profile-wizard.html` | 8 | Multi-step Blazor wizard; publish = save profile |
| `application-profile-usage.html` §1 | 8 | Same |
| `application-profile-usage.html` §2–3 | 9 | Create flow + read-only profile summary on Application |
| `application-detail-m2m.html` | 10 | Custom DetailView, wide SQL view, auto-resolve |
| `images/ap-*.png` | 8–10 | Refresh when officer UX ships |

---

## Suggestion cheat sheet (quick)

| Officer intent | Profile knobs |
|----------------|---------------|
| Standard employee visa via ministries | `ForEmployee`, `Issuance`, `ViaMinistries`, `ProduceVisa`, passport + position toggles |
| Cancel existing visa | `Cancellation`, `CancelVisas` |
| Register family member | `Registration`, `ForFamilyMember`, FM suggest on start (§11) |
| Business trip | `BusinessTrip`, trip address / region per-App fields |
| Direct to migration | `Direct migration` route, no ministry legs on profile |
| Locked profile variant | Duplicate profile → edit copy → use for new Applications only |
