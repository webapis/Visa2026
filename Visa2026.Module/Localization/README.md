# Layer A UI translations (A3 + A4)



**Sources:**



| File | Role |

|------|------|

| `UiStrings.json` | Core navigation, actions, main BOs (`Application`, `Person`, …), primary views |

| `tools/GenerateModelLocalization/UiStrings.entities.json` | Remaining business objects + lookups (`commonMembers` for lookup types) |

| `tools/GenerateModelLocalization/UiStrings.views-a4.json` | Extra nav, actions, detail layouts, list columns, per-class member overrides |
| `tools/GenerateModelLocalization/UiStrings.logon.json` | Login form (`AuthenticationStandardLogonParameters_*_DetailView`, UserName, Password, LogonText) |
| `tools/GenerateModelLocalization/UiStrings.person-detail.json` | Person detail nested tabs, collection list columns (Education, Passport, …), PDF actions, `ExpirationState` enum |



English defaults remain in code / base `Model.xafml`.



**Generate** localization model files after editing any JSON above:



```powershell

dotnet run --project tools/GenerateModelLocalization/GenerateModelLocalization.csproj

```



**Outputs:**



| File | Role |

|------|------|

| `Model.DesignedDiffs.Localization.{culture}.xafml` | Module embedded resources (tr-TR, tk-TM, ru-RU) |

| `Visa2026.Blazor.Server/Model.{culture}.xafml` | Host-specific nav (Visa Extension) + app title |



Rebuild the solution after regeneration.



**A4 scope:** entity captions + auto `{Type}_ListView` / `{Type}_DetailView`; lookup sub-navigation (Housing, Passport, Visa, …); layout tab captions on Person / Visa / WorkPermit and related views; visa & work-permit tracking list columns.

**Nested navigation:** use a `caption` + `children` object under a group id (e.g. `People` → `Employees`, `FamilyMembers`) so localized xafml matches the model tree.



**A5 (partial):** `ShowInReport`, `ShowInDocument` (model actions); grid search placeholder via `GridSearchBoxLocalizationController` + `VisaLocalization.GetGridSearchBoxNullText()`.

**Still A5:** controller `ShowMessage` strings, validation rule text, other DevExpress built-ins.


