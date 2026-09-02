# Resminamalar — This profile / Shared tabs (prototype)

Officer catalog on a case. Shared include/exclude happens here with a toggle, not only on the Application Profile wizard.

Status: **shipped** (2026-09-02). Officer sign-off on the prototypes; case Resminamalar uses **This profile** / **Shared** tabs with an ON/OFF include toggle.

## Screens

| File | State |
|------|--------|
| [resminamalar-this-profile-tab-prototype.png](./resminamalar-this-profile-tab-prototype.png) | **This profile** tab. This-profile files plus shared files that are ON. Shared rows show a **SHARED** chip. Checkboxes, READY/CHECK, Preview, Download package stay here. Create template / Add existing stay on this tab. |
| [resminamalar-shared-tab-toggles-prototype.png](./resminamalar-shared-tab-toggles-prototype.png) | **Shared** tab. Library list. Green **ON** / grey **OFF** pill toggle (officer-supplied control). ON also appears on This profile, marked Shared. No ZIP checkboxes on this tab. |

## Locked for this prototype

| Topic | Choice |
|-------|--------|
| Tabs | **This profile** and **Shared** under the catalog title. Recycle Bin stays a utility link, not a third content tab. |
| Toggle | Shared tab only. ON = include for this case/profile. OFF = exclude. |
| This profile list | Nested this-profile templates plus ON shared rows. Shared rows marked **SHARED**. |
| Authoring | Create template / Add existing / yellow marks stay on This profile (and Recycle Bin). |

## Not in this set

Empty Shared tab. Toggle confirmation. Wizard Include/Exclude removed (already view-only for this-profile).

## Domain

Resminamalar catalog (`ApplicationReportPackageComponent`). Shared rows are `UserReportTemplate` include on the profile, same as wizard Include/Exclude.