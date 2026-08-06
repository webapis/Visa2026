# Application Profile — configuration & clone model (plan)

**Status:** Prototype + questions (no domain implementation yet)  
**Prototype:** [`docs/prototypes/application-profile-wizard.html`](prototypes/application-profile-wizard.html)  
**Input draft:** `Application.xlsx` (attached to planning chat)  
**Related today:** `ApplicationType` (`LookupBusinessObjects.cs`), `Application`, `ApplicationItem`, `ApplicationProgress`, `ApprovalLegProfile`, `UserReportTemplate`, `ProjectContract`

---

## 1. Problem

Application-type behavior is **scattered**:

| Concern | Where it lives today |
|--------|----------------------|
| UI visibility / required fields | Dozens of `Show*` flags on `ApplicationType` + `[Appearance]` on `Application` / `ApplicationItem` |
| Issue capabilities | `CanIssueVisa` / `CanIssueInvitation` / `CanIssueWorkPermit` (+ catalog JSON) |
| Progress route / ministry depth | `ApplicationProgressRoute`, `MinistryReviewDepth` on type; legs on `ApprovalLegProfile` / `ProjectContract` |
| Defaults (visa type/period/…) | Hard-coded in `Application.cs` by type `Name` |
| Report applicability | `UserReportTemplate` links to types / type groups / project contracts |
| SLA | `ApplicationMigrationSlaProfile` (+ duration fields) |
| Selection UX | `SelectionCode` + quick code (see `APPLICATION_BO_TYPE_SELECTION_REFACTOR.md`) |

Creating or tweaking a “new kind of application” requires editing lookup flags, seed JSON, Appearance criteria, and often C# defaults — poor officer/admin UX and hard to reuse.

---

## 2. Goal

Introduce **Application Profile** as the single configuration surface (wizard) that defines how an Application behaves.

- **Rename conceptually:** Application Profile **replaces** today’s `ApplicationType` as the thing officers configure and Applications bind to.
- **Template + clone:**
  - Source profile = reusable template (created once).
  - On Application create, profile is **cloned** onto the Application.
  - Clone edits **do not** change the source template.
  - Template edits apply to **future** clones only.
- **Scope:** profiles may be general or scoped (Application purpose only, or + ProjectContract, ApprovalLegProfile, etc. — unbounded dimensions).
- **Wizard content** follows `Application.xlsx` sections (identity, results/fields, process/SLA, templates, person data).

**First deliverable (this PR):** visual wizard prototype only — not EF/BOs.

---

## 3. Excel → wizard mapping

| Excel section | Wizard step (prototype) | Notes vs current model |
|---------------|-------------------------|------------------------|
| Name / Description / Code | Step 1 — Identity | Replaces `LookupBase` name/code (+ optional `SelectionCode`) |
| Directed to: ministry / migration | Step 1 — Route | `ApplicationProgressRoute` |
| For: Employee / FM / Temporary visitor | Step 1 — Audience | Extends/replaces `ApplicationTypeCategory` |
| Related to: issuance / cancel / registration / business trip | Step 1 — Action family | Partly overlaps lifecycle + ShowRegistrations / ShowBusinessTrips |
| Result may produce | Step 2 — Produce | `CanIssue*` + border zone / work location (new explicit axes) |
| Result may cancel | Step 2 — Cancel | Today mostly implicit via cancel application types |
| Properties required + defaults | Step 2 — Fields | Replaces `Show*` + hard-coded defaults in `Application.cs` |
| Signatory / representative | Step 2 — Signatory | New first-class defaults (org singletons exist) |
| Approval legs + states + SLA | Step 3 — Process | Absorbs legs/SLA now split across type, profile, contract |
| Application templates | Step 4 — Templates | Invert ownership vs `UserReportTemplate` applicability lists |
| Required person-related data | Step 4 — Person | Passport / education / position / address — today many item `Show*` flags |

---

## 4. Proposed domain shape (sketch — not implemented)

```
ApplicationProfile          // source template (admin/config)
  - Identity, route, audience, action family
  - Produce / cancel capability sets
  - Field requirements + default values
  - Signatory defaults
  - Process: legs, allowed states, SLA days
  - Template links
  - Person data requirements
  - Optional scope predicates (contract, etc.)
  - IsActive, Version?

ApplicationProfileClone     // owned by Application (or embedded JSON / owned aggregate)
  - Snapshot of profile at create time
  - Mutable per Application
  - SourceProfileId (nullable FK for lineage; not live-bound)

Application
  - ApplicationProfileClone (required after migration)
  - (transitional) ApplicationType retained until cutover
```

**Clone depth:** deep enough that process legs, field defaults, template list, and person requirements on the Application are independent of later template edits.

---

## 5. Migration posture (high level)

1. Prototype UX (this PR) → lock wizard sections with stakeholders.  
2. Introduce `ApplicationProfile` alongside `ApplicationType`; seed profiles from existing type catalog.  
3. Dual-read: Application still has Type; profile clone derived from type.  
4. Switch Appearance / progress / reports to read clone.  
5. Deprecate `ApplicationType` / type groups / type-linked template filters.  
6. Remove hard-coded type-name defaults from `Application.cs`.

Exact cutover and data migration are **out of scope** until questions below are answered.

---

## 6. Non-goals (for now)

- Implementing EF entities, updaters, or XAF DetailViews for the wizard.
- Changing VISA2014 import mapping.
- Replacing Resminamalar / Document copies engines (only how templates are *associated*).

---

## 7. Open questions

### A. Product / naming
1. Confirm **Application Profile** replaces **ApplicationType** in officer language (nav, manuals, reports) — keep internal `ApplicationType` table only during migration?
2. Is “Temporary visitor” a real third audience, or a placeholder for a later person category?

### B. Clone semantics
3. On Application create: always clone entire profile, or allow “link to live template” for some tenants?
4. Can officers **re-sync** a clone from the updated source (overwrite with confirm), or is divergence permanent until manual edit?
5. Is the clone a **full owned BO graph** (queryable in XAF) or a **serialized snapshot** (JSON) with a thin editor?

### C. Scope
6. How is multi-scope expressed? (e.g. profile applicable when `ProjectContract` matches **or** when approval-leg profile matches — AND vs OR? UI for arbitrary criteria?)
7. Does scope filter which profiles appear in the Application picker only, or also constrain runtime validation?

### D. Excel vs current capabilities
8. Excel “related to” is a **single** radio (issuance | cancel | registration | business trip). Today one type can combine flags (e.g. show registrations + issue visa). Keep exclusive families, or allow multi-select?
9. “Cancel existing Application(s)” as radio in Excel — intentional single-select vs checkboxes?
10. Which Excel “required properties” map 1:1 to Application header vs ApplicationItem (e.g. entry date / checkpoint today live on registration lines)?

### E. Process
11. Do approval legs on the profile **replace** `ApprovalLegProfile` / contract legs, or reference an existing `ApprovalLegProfile` as a building block?
12. Excel ministry/migration **state checklists** — freeform enablement of `ApplicationState` rows, or stay on today’s fixed route transition tables (`ApplicationProgressProfileResolver`)?
13. SLA: replace `ApplicationMigrationSlaProfile` tiers with raw day integers on the profile?

### F. Templates & person
14. Should profiles **own** template attachments, while `UserReportTemplate` remains the file/placeholder store — or fully nest files inside the profile?
15. Person requirements: only the four Excel checkboxes for v1, or expand to the full current `ShowCurrent*` matrix in a later step?

### G. Permissions & UX
16. Who edits source profiles — Administrators only, or selected officers?
17. Prefer **wizard** (as prototype) vs single long DetailView with tabs in XAF for v1?

---

## 8. Suggested next steps after answers

1. Lock decisions for A–C (naming + clone model + scope).  
2. Produce a field dictionary: Excel row → profile property → current `Show*` / capability.  
3. Thin vertical slice: `ApplicationProfile` BO + clone on Application create (read-only clone on Application) — still no full wizard.  
4. Migrate one sample type (e.g. invitation+WP) end-to-end as proof.
