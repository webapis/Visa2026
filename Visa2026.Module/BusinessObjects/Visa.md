# Business Object: Visa

## 1. Purpose

The `Visa` business object stores information about a travel visa issued for a specific passport. Issuance may be tied to the application workflow via optional **`IssuingApplicationItem`** and **`InvitationItem`** links (see §3). Both appear on the detail view behind the **optional-fields gear** when collapsed; they auto-expand when populated.

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
| `BorderZoneLocation` | `string` | Comma-separated border zone labels (`BorderZoneName` catalog). Required; defaults to **`Ýok`** when unset (no border zones). | `[RuleRequiredField]`; multi-select sentinel `Ýok`. |
| `Passport` | `Passport` | Passport this visa is stamped on. | Required. |
| **`AvailableIssuingApplicationItems`** | — | **Not mapped.** Filtered **`ApplicationItem`** list: same **`Person`** as **`Passport`**, **`ApplicationType.Name`** in **`VisaIssuingApplicationTypes`**, not soft-deleted. Drives **`IssuingApplicationItem`** lookup (`[DataSourceProperty]`). |
| **`IssuingApplicationItem`** | **`ApplicationItem`** | **The application line for the visa holder** under which this visa was issued. | **Optional** (gear). When set: same **`Person`** as **`Passport.Person`**, allowed **`ApplicationType.Name`**. UI caption: *Issuing Application Item*. |
| **`AvailableInvitationItems`** | — | **Not mapped.** **`InvitationItem`** rows for **`Passport.Person`**. Drives **`InvitationItem`** lookup. |
| `InvitationItem` | `InvitationItem` | Linked invitation line item for the visa holder. | **Optional** (gear). Person must match **`Passport.Person`** when set. |
| `AssociatedApplicationItems` | `IList<ApplicationItem>` | Application items that reference this visa as their **target / current** visa (`ApplicationItem.CurrentVisa`). Inverse of `CurrentVisa`. | Optional collection. **`[VisibleInDetailView(false)]`** — not shown on Visa Detail View; linkage kept for logic/reports. |
| `Images` | `IList<VisaImage>` | Scans of the visa. | Aggregated. |
| `Documents` | `IList<VisaDocument>` | Related documents. | Aggregated. |
| `Notes` | `string` | Free text. | Optional (gear). |
| `IsCancelled` | `bool` | Visa cancelled (also set via sync rules from cancel applications). | Optional (gear); editable on detail view. |
| `IsChanged` | `bool` | Visa superseded by a change application. | Optional (gear); editable on detail view. |
| `IsExtended` | `bool` | Visa extended via extension workflow. | Optional (gear); editable on detail view. |
| `ExtensionRequired` | `bool` | Whether extension is still needed. | Optional (gear); default **true** on new records. |
| `RegistrationState` | `string` | Computed registration context (read-only in UI). | DB computed / not user-edited. |

**Active visa (`SingleActiveBaseObject`):** At most one visa per person may be the active/current visa; behavior follows `SingleActiveBaseObject<Person, Visa>` (see `SingleActiveBaseObject.md`). **`IsActive`** and list coloring follow the usual pattern for this hierarchy.

**Soft delete:** Implements `ISoftDelete` (`IsDeleted`, `DateDeleted`, `DeletedBy`) where applicable.

**Removed (deprecated):** `HasInvitation`, `HistoricalImport` — visibility and optionality are handled by **`ShowOptionalFields`** / optional detail fields; columns dropped by **`VisaVisibilityToggleColumnsCleanupUpdater`**.

---

## 3. Application linkage (business rule)

| Concept | Meaning |
|--------|---------|
| **`IssuingApplicationItem`** | Points to the **`ApplicationItem`** whose **`Person`** must be **the visa holder** (`Passport.Person`). It is **that person’s line** on an **`Application`** that issued this visa—not another applicant’s line on the same batch. **May be omitted** (e.g. pre-system import, manual entry). |
| **`InvitationItem`** | Optional link to the person’s invitation line used for this visa. |
| **`AssociatedApplicationItems`** | Other **`ApplicationItem`** rows that **use this visa as `CurrentVisa`** (e.g. extensions, cancellations, downstream steps). One visa may appear here on zero or many items. |

**Rule:** **`IssuingApplicationItem`** and **`InvitationItem`** are **not required** at save. When **`IssuingApplicationItem`** is set, person and application-type checks apply.

**Allowed issuing application types (when issuing item is used):** The parent **`Application`**’s **`ApplicationType.Name`** must be in **`VisaIssuingApplicationTypes`** (invitation flows, extensions, exit visa, passport change, etc.).

---

## 4. Validation (high level)

| Mechanism | Purpose |
|-----------|---------|
| `IsPersonValid` (`RuleFromBoolProperty`) | When **`IssuingApplicationItem`** is set, ensures **`IssuingApplicationItem.Person`** matches **`Passport.Person`**. |
| `IsIssuingApplicationTypeAllowed` (`RuleFromBoolProperty`) | When **`IssuingApplicationItem`** is set, ensures **`ApplicationType.Name`** is in **`VisaIssuingApplicationTypes`**. |
| `IsInvitationPersonValid` | When **`InvitationItem`** is set, ensures invitation person matches **`Passport.Person`**. |

Lookup choices for **`IssuingApplicationItem`** come from **`AvailableIssuingApplicationItems`**; for **`InvitationItem`** from **`AvailableInvitationItems`** (**`[DataSourceProperty]`**).

---

## 5. UI & Behavior Notes

- Navigation: **Lookup/Visa**.
- **`ExpirationDate`** must be later than **`StartDate`**.
- Optional fields (including **`IssuingApplicationItem`**, **`InvitationItem`**, status flags, **`Notes`**) use the **gear toggle** — see **`docs/OPTIONAL_DETAIL_FIELDS.md`**.
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
- **`docs/OPTIONAL_DETAIL_FIELDS.md`** — gear toggle behavior on **`Visa`** detail view.
