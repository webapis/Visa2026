# Reference: Application Profile

Companion to [SKILL.md](./SKILL.md).

---

## File map

| Area | Path |
|------|------|
| Profile BO + children | `Visa2026.Module/BusinessObjects/ApplicationProfile.cs` |
| Workspace host BO | `Visa2026.Module/BusinessObjects/ApplicationWorkspace/ApplicationWorkspaceHost.cs` |
| Workspace query service | `ApplicationWorkspaceQueryService.cs` (live; mock retained for fallback) |
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
| Profile overview (live) | `ApplicationProfileOverviewQueryService`, `ApplicationProfileOverviewComponent.razor` |
| Profile picker at create | `ApplicationProfilePicker*`, `ApplicationProfilePickerNewController` |
| Officer manual | `user-manual/docs/en/guides/applications/application-profiles.md`, `administration/configuration/application-profiles.md` |
| Registry | `docs/DEPRECATED.md` |
| Plan | `docs/APPLICATION_PROFILE_PLAN.md` |

### Officer shell (Blazor B0–B8)

| Area | Path |
|------|------|
| Native Application Profiles nav | `CustomNavigationUpdater`, `ApplicationProfileCatalogModelUpdater`, `ApplicationStagedStartProcessController`, `ApplicationProfileInstanceProgressRouteNavigation` |
| Shell host BO + view id | `Visa2026.Module/BusinessObjects/OfficerShell/OfficerShellHost.cs`, `OfficerShellViewIds.cs` |
| Shell property editor + model | `Visa2026.Blazor.Server/Editors/OfficerShellPropertyEditor.cs`, `OfficerShellModel.cs`, `OfficerShellComponent.razor` |
| Staged / in-process queues | `OfficerShellStagedQueryService`, `OfficerShellInProcessQueryService`, `OfficerShellStartProcessService` |
| Case workspace tabs | `OfficerShellCaseWorkspaceComponent.razor`, `OfficerShellCase*Tab.razor` (overview, people, progress, documents, resminamalar, SLA) |
| Case snapshot builder | `ApplicationWorkspaceCaseBuilder.cs`, `ApplicationWorkspaceCaseModels.cs` |
| Person link picker (B8) | `IApplicationPersonLinkQueryService`, `OfficerShellPersonLinkPickerComponent.razor` |
| Case progress in-shell (B7) | `IOfficerShellCaseProgressService`, `OfficerShellCaseProgressTab.razor` |
| Immersive tab-bar hide (B6) | `OfficerShellImmersiveTabBarController.cs`, `officer-shell-host.css` |
| Person detail open (no dispose) | `PersonDetailOpenHelper.cs` |
| Preview slot requests | `IVisaPreviewSlotService.cs` — `ResminamalarSlotRequest`, `DocumentCopiesSlotRequest`, `ProgressLettersSlotRequest` (`OpenPreviewOnly`, roster scope) |
| Roster document copies merge | `ApplicationItemDocumentCopyPdfMerger.TryBuildMergedPdfForRoster` |
| HTML prototype (parity) | `wwwroot/officer-shell/`, `parity/CHECKLIST.md` |

### Application workspace (legacy XAF path)

| Area | Path |
|------|------|
| Workspace host | `Visa2026.Module/BusinessObjects/ApplicationWorkspace/ApplicationWorkspaceHost.cs` |
| Live query + tabs | `ApplicationWorkspaceQueryService.cs`, `ApplicationWorkspaceTabBuilder.cs` |
| Workspace Blazor UI | `ApplicationWorkspacePropertyEditor.cs`, `ApplicationWorkspaceComponent.razor` |
| Open helpers | `ApplicationWorkspaceOpenHelper.cs`, `ApplicationWorkspaceDocumentCopiesOpenHelper.cs`, `ApplicationWorkspaceResminamalarOpenHelper.cs` |

### Planned (phase B — not yet)

| Area | Expected path |
|------|----------------|
| Hard-remove `ApplicationItem` BO/schema | After VISA2014 import + Report Dashboard cutover (slice 13b) |

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

| PNG mockup (`docs/prototypes/`) | Slice | Deliverable |
|----------------------------------|-------|-------------|
| `application-profile-template-wizard*.png` (steps 1–5) | 8 | Multi-step Blazor template wizard; publish = save profile |
| `application-profile-templates-*.png`, `application-profile-template-overview-mockup.png` | 8c | Custom template catalog + overview |
| `staged-profiles-*.png` | 10+ | Staged profile queue (list/grid, Start process) |
| `process-started-profiles-*.png`, `process-started-application-profile-workspace-mockup.png`, `process-started-nav-*.png` | 10 / B5 | In-process case workspace (officer shell 6 tabs) |
| `visa2026-custom-left-navigation-shell-mockup.png` | B0–B3 | Blazor officer shell replaces native XAF left nav + immersive chrome |

**Retired:** `application-profile-wizard.html`, `application-profile-usage.html`, `application-detail-m2m.html`, `images/ap-*.png`, Excel draft (removed 2026-08-10).

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
