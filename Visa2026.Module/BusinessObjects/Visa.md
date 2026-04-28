# Business Object: Visa

## 1. Purpose

The `Visa` business object stores information about a travel visa issued for a specific passport. Normally, issuance is tied to the application workflow via **`IssuingApplicationItem`** (see §3). For **pre-system or legacy** records where no application exists in this system, **`HistoricalImport`** may be set so **`IssuingApplicationItem`** is optional and hidden in the UI.

---

## 2. Properties

Key fields as implemented in `Visa.cs` (not exhaustive for derived/UI-only members):

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `VisaNumber` | `string` | Unique number identifying the visa sticker/document. | Required; max 50 chars. |
| `VisaType` | `VisaType` | Classification of the visa. | Required. |
| `VisaCategory` | `VisaCategory` | Category (e.g. entries, purpose). | Required. |
| `VisaIssuedPlace` | `VisaIssuedPlace` | Where the visa was issued. | Required. |
| `IssueDate` | `DateTime` | Issue date (`IssueDate` property uses a backing field; see §5.1). | Required; **`[ImmediatePostData]`**. |
| `StartDate` | `DateTime` | Validity start. | Required; **`[ImmediatePostData]`**. Often matches issue date — see §5.1. |
| `ExpirationDate` | `DateTime?` | Validity end. | Required; must be greater than `StartDate` (`RuleCriteria`). |
| `HasBorderZonePermit` | `bool` | Whether a border zone permit applies. | — |
| `BorderZoneLocations` | `IList<City>` | Allowed border zone cities when permit applies. | Required when `HasBorderZonePermit` is true. |
| `HasInvitation` | `bool` | Whether an invitation is linked. | — |
| `InvitationItem` | `InvitationItem` | Linked invitation line item. | Required when `HasInvitation` is true. |
| `Passport` | `Passport` | Passport this visa is stamped on. | Required. |
| `HistoricalImport` | `bool` | **True** = no issuing application on file (e.g. migrated pre-system visa). **False** (default) = normal workflow. | Default **`false`** (**`OnCreated`**); user may set **`true`** when application data is unavailable. **`[ImmediatePostData]`** for UI refresh. |
| **`AvailableIssuingApplicationItems`** | — | **Not mapped.** Filtered **`ApplicationItem`** list: same **`Person`** as **`Passport`**, **`ApplicationType.Name`** in **`VisaIssuingApplicationTypes`**, not soft-deleted. Drives **`IssuingApplicationItem`** lookup (`[DataSourceProperty]`). |
| **`IssuingApplicationItem`** | **`ApplicationItem`** | **The application line for the visa holder:** same **`Person`** as **`Passport.Person`**, allowed **`ApplicationType.Name`**, parent **`Application`**. | **Required when `!HistoricalImport`**. Hidden on Detail View when **`HistoricalImport`** is true. Choices from **`AvailableIssuingApplicationItems`**. UI caption: *Issuing Application Item*. |
| `AssociatedApplicationItems` | `IList<ApplicationItem>` | Application items that reference this visa as their **target / current** visa (`ApplicationItem.CurrentVisa`). Inverse of `CurrentVisa`. | Optional collection. **`[VisibleInDetailView(false)]`** — not shown on Visa Detail View; linkage kept for logic/reports. |
| `Images` | `IList<VisaImage>` | Scans of the visa. | Aggregated. |
| `Documents` | `IList<VisaDocument>` | Related documents. | Aggregated. |
| `Notes` | `string` | Free text. | Optional. |
| `RegistrationState` | `string` | Computed registration context (read-only in UI). | DB computed / not user-edited. |

**Active visa (`SingleActiveBaseObject`):** At most one visa per person may be the active/current visa; behavior follows `SingleActiveBaseObject<Person, Visa>` (see `SingleActiveBaseObject.md`). **`IsActive`** and list coloring follow the usual pattern for this hierarchy.

**Soft delete:** Implements `ISoftDelete` (`IsDeleted`, `DateDeleted`, `DeletedBy`) where applicable.

---

## 3. Application linkage (business rule)

| Concept | Meaning |
|--------|---------|
| **`IssuingApplicationItem`** | Points to the **`ApplicationItem`** whose **`Person`** must be **the visa holder** (`Passport.Person`). It is **that person’s line** on an **`Application`** that issued this visa—not another applicant’s line on the same batch. **Omit** (and set **`HistoricalImport`**) when no application data exists. |
| **`HistoricalImport`** | When **true**, **`IssuingApplicationItem`** is not required; person/type checks apply only if an issuing item is still set. |
| **`AssociatedApplicationItems`** | Other **`ApplicationItem`** rows that **use this visa as `CurrentVisa`** (e.g. extensions, cancellations, downstream steps). One visa may appear here on zero or many items. When not historical, **issuing** is exactly one **`IssuingApplicationItem`** at save. |

**Rule (normal):** Without **`HistoricalImport`**, a saved **`Visa`** must have **`IssuingApplicationItem`** set—the usual business rule is issuance in application context.

**Rule (legacy):** With **`HistoricalImport` true**, **`IssuingApplicationItem`** may be null (e.g. bulk import of pre-system visas).

**Allowed issuing application types (when issuing item is used):** The parent **`Application`**’s **`ApplicationType.Name`** must be in **`VisaIssuingApplicationTypes`** (invitation flows, extensions, exit visa, passport change, etc.).

---

## 4. Validation (high level)

| Mechanism | Purpose |
|-----------|---------|
| `IsPersonValid` (`RuleFromBoolProperty`) | When **`IssuingApplicationItem`** is set, ensures **`IssuingApplicationItem.Person`** matches **`Passport.Person`**. Skipped when **`HistoricalImport`** and **`IssuingApplicationItem`** is null. |
| `IsIssuingApplicationTypeAllowed` (`RuleFromBoolProperty`) | When **`IssuingApplicationItem`** is set, ensures **`ApplicationType.Name`** is in **`VisaIssuingApplicationTypes`**. Skipped when **`HistoricalImport`** and **`IssuingApplicationItem`** is null. |
| `IsInvitationPersonValid` | When **`HasInvitation`** and **`InvitationItem`** are set, ensures invitation person matches **`Passport.Person`**. |

Lookup choices for **`IssuingApplicationItem`** come from **`AvailableIssuingApplicationItems`** (**`[DataSourceProperty]`**), which queries **`VisaIssuingApplicationTypes`** (edit **`Names`** in code) plus passport person — works on Blazor; string-based **`DataSourceCriteria`** reflection does not.

---

## 5. UI & Behavior Notes

- Navigation: **Lookup/Visa**.
- **`ExpirationDate`** must be later than **`StartDate`**.
- Border zone UI: **`BorderZoneLocations`** visible/required when **`HasBorderZonePermit`** is true; invitation UI when **`HasInvitation`** is true.
- **`HistoricalImport` true:** **`IssuingApplicationItem`** hidden on Detail View (**`Appearance`**); **`AvailableIssuingApplicationItems`** returns an empty list.
- List views: deleted rows grayed out; severity-based row coloring via state evaluation (`StateSeverityLevel`).
- Only one active visa per **Person** at a time (see `SingleActiveBaseObject`).

### 5.1 Issue Date → Start Date suggestion

When **`IssueDate`** changes, **`StartDate`** is set to the same **calendar date** **only if**:

- **`StartDate`** is still **default** (`0001-01-01`), **or**
- **`StartDate`** is still on the **same calendar day** as the **previous** **`IssueDate`** (they were aligned).

If the user has already set **`StartDate`** to a **different** day than the previous issue date, **`StartDate`** is **not** overwritten — deferred validity start stays user-controlled.

Implemented in the **`IssueDate`** setter in `Visa.cs`; **`CrossObjectSyncHelper.SyncOnPropertyChanged`** runs when **`ObjectSpace`** is available.

---

## 6. Related documentation

- **`VisaIssuingApplicationTypes.cs`** — canonical allowlist (**`Names`** array); **`AllowedApplicationTypeNames`** used for validation and **`Visa.AvailableIssuingApplicationItems`**.
- **`ApplicationItem.md`** — **`CurrentVisa`** vs **`Visa.IssuingApplicationItem`** (target vs issuing).
- **`APPLICATION.md`** — parent **`Application`** and **`ApplicationItems`** collection.
- **`docs/BUSINESS_LOGIC_BASELINE.md`** — traceability and linkage to applications.
