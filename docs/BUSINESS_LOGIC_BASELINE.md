# Business Logic Baseline (Business -> Objects -> Code)

Status: Draft v0.1  
Owner: Product + Domain Expert + Tech Lead  
Last Updated: 2026-04-26

---

## 1) Core Business Problem (Start Here)

### 1.1 Problem Statement
Finalized from stakeholder input:

`The visa department needs one system to prepare and track application forms and process states (on process, processed, cancelled, rejected, etc.) for invitations, work permits, visa extensions, border zone permits, check-in/check-out, and other immigration processes for foreign employees and their family members, while also tracking document states (validity, expiration, extension) and producing operational reports.`

### 1.2 Why This Product Exists
- Prevent legal/compliance breaches (expired visa/registration/work permit).
- Coordinate multi-step authority processes through one operational workflow.
- Turn document and application timelines into proactive actions.
- Keep a traceable history for audits and management decisions.
- Legal/process context for v1: Turkmenistan only.

### 1.3 Target Users and Jobs-To-Be-Done
- Primary users: Visa Officers in the Visa Department.
- Primary approver/supervisor: Visa Chief of the Visa Department.
- Operational need: know what action is needed now, for whom, and with what priority.
- Management need: monitor throughput, bottlenecks, and compliance risk.
- Direct non-primary users: Not yet determined (TBD).
- Indirect stakeholders: Migration Service of Turkmenistan, partner ministries the company works with, and company chief/executive leadership.

### 1.4 Success Outcomes (Business KPIs)
- Ability to prepare various application forms related to migration procedures.
- Ability to track migration procedure states end-to-end.
- Ability to track validity states of core documents (visa, passport, invitation, work permit, etc.).
- Primary 3-month success measure: user satisfaction based on ability to clearly see and trust states of application forms, visa, passport, invitation, work permit, medical records, and other migration-regulation documents in one system.
- % reduction in expired/overdue cases.
- % of renewals submitted within required window (e.g., 90-day extension window).
- Average days from "required action" to "application submitted".
- Rejection rate by application type and reason.
- Dashboard-to-list consistency rate (operational trust metric).

### 1.5 Current Top Pain Points (As-Is)
1. Difficult tracking of application form processing states.
2. Difficult tracking of document/process objects: visa, invitation, work permit, border zone permit, and passport.
3. Difficult tracking of registration lifecycle states (check-in/check-out).

### 1.6 Purpose and Benefits of State Tracking
- Provide early warning before compliance breaches (expiring/overdue/cancelled cases).
- Make required next actions clear for Visa Officers and Visa Chief.
- Show real process location/progress across office, ministry, and migration service stages.
- Preserve auditable process history and decision timeline through application progress tracking.
- Reduce manual tracking errors by using deterministic business rules.
- Enable fast operations: click a state on dashboard and open exact affected records.
- Keep business logic and implementation aligned through explicit, testable state definitions.

### 1.7 Main Components of State Tracking
- Tracked business objects: visa, passport, work permit, invitation, registration, and related person-linked records.
- State definitions: clear validity states and process states with business meaning.
- State evaluation rules: deterministic logic with precedence (for example cancellation/extension evidence vs raw date checks).
- Process history engine: `ApplicationProgress` timeline where latest date/time entry is authoritative for current process state.
- Cross-object linkage model: `ApplicationItem` links person/object records to specific application flows (for example `CurrentVisa` linkage).
- **Visa issuance traceability**: A saved **`Visa`** may reference **`IssuingApplicationItem`** (the **`ApplicationItem`** whose parent **`Application`** produced the visa) when that data exists; it is optional on the detail view (gear toggle). Legacy/pre-system visas may omit it. **`ApplicationItem.CurrentVisa`** remains the complementary link when an application line **references** a visa as its target (inverse: **`Visa.AssociatedApplicationItems`**). See **`Visa2026.Module/BusinessObjects/Visa.md`** §3.
- Operational visibility layer: State Dashboard with strict click-to-open exact-state record navigation.
- Governance layer: baseline business-logic documentation as source of truth for rule-to-code alignment.

### 1.8 State Calculation Complexity Levels
- Level 1 (simple): single-BO, single-condition checks (for example date-based expiry only).
- Level 2: single-BO with multiple flags and local priority rules.
- Level 3: cross-BO linkage-dependent evaluation (for example object + linked application context).
- Level 4: timeline-aware process evaluation (latest progress entry, terminal vs non-terminal states).
- Level 5 (most complex): multi-path evidence-based logic with precedence overrides (for example cancellation evidence and issued-linkage evidence overriding date-only interpretation).

For this project:
- Visa state determination is high-complexity (near Level 5) because it combines validity dimension + process dimension + cross-object evidence + precedence rules.

Implementation/testing implications:
- Keep precedence order explicitly documented and stable.
- Test link-driven scenarios (`ApplicationItem` and `Registration` paths).
- Test lag/inconsistency scenarios where process codes and linkage evidence diverge.
- Keep dashboard count logic and detail filter logic equivalent for each state.

### 1.9 State Rule Test Plan (How We Verify)
- Layer 1: unit tests for evaluator/rule methods with deterministic inputs and expected state outputs.
- Layer 2: integration tests for cross-BO linkage logic (`ApplicationItem`, `Registration`, cancellation/extension evidence, latest `ApplicationProgress`).
- Layer 3: seeded data-scenario tests (one scenario per critical state/rule path) validating resulting state assignment.
- Layer 4: dashboard parity tests ensuring state tile counts equal records opened by clicking that state (strict exact-match behavior).

High-priority edge cases (especially for Visa):
- cancellation evidence exists while date logic suggests another state.
- issued-linkage evidence exists while process outcome code is not yet updated.
- expiring-soon and on-extension should still remain within active visa population.
- multiple related applications with mixed statuses for one visa/person.
- one active valid visa per person constraint.

### 1.10 State Inventory Before Implementation (Required)
Before implementing or changing any state logic, maintain an approved state inventory.

Required inventory columns:
- `StateCode` (unique, canonical)
- `StateDisplayName`
- `BusinessObject`
- `StateDimension` (`Validity`, `Process`, `Compliance`, or `Mixed`)
- `BusinessMeaning` (one-line definition)
- `TriggerSummary`
- `PriorityOrder`
- `DataSource` (`BO` evaluator or `SQL` view)
- `OwnerBusiness` (who approves business meaning)
- `OwnerTechnical` (who implements/maintains)
- `Status` (`Planned`, `In Progress`, `Implemented`, `Deprecated`)

Mandatory governance rules:
- One state code must map to exactly one business meaning.
- No implementation starts before state inventory entry is approved.
- New states must include dashboard behavior and expected filter result definition.
- State naming must follow section/object naming convention consistently.
- Duplicate semantic states must be merged into one canonical state.

---

## 2) In/Out of Scope

### In Scope
- Immigration lifecycle tracking for foreign employees.
- Document validity + workflow progress + location/status in authorities.
- Alerting/escalation states and operational dashboards.
- Rule-based gating (e.g., passport validity blocks extension).
- V1 focus: core tracking and application form preparation only.

### Out of Scope (confirm)
- Government-side processing systems.
- Payroll/benefits domain logic.
- Full legal advisory automation.
- Non-essential advanced features that make phase 1 complicated; prioritize simple and usable workflows first.
- Advanced analytics/reporting and external integrations/automation in v1.

---

## Confirmed Decisions (Interview-Based)

These decisions are confirmed by stakeholder answers and should be treated as baseline requirements for v1.

### Product Scope and Users
- Project serves the Visa Department.
- Primary users are Visa Officers; primary supervisor is Visa Chief.
- Indirect stakeholders include Migration Service of Turkmenistan, partner ministries, and company leadership.
- Legal/process context for v1 is Turkmenistan only.
- v1 scope is core tracking and application form preparation; avoid non-essential complexity.

### State Management and UX
- Business objects should support both validity-state and process-state tracking where applicable.
- Dashboard should show both dimensions together when available.
- Dashboard is the primary navigation for state management.
- Clicking a dashboard state should open exact-match filtered records (strict match).

### Key Object/Relationship Decisions
- `Person` represents both foreign employees and employee family members (`IsEmployee` differentiates).
- `Person -> Passport`: one active current passport expected, with history.
- `Person -> Visa`: one active current visa expected, with history.
- `Person -> WorkPermitItem`: multiple active permits may exist simultaneously.
- `Person -> Invitation`: multiple active invitations may exist simultaneously.
- `Person -> BorderZonePermit`: multiple active permits may exist simultaneously.
- `Person -> MedicalRecord`: one active current record expected, with history.
- `Person -> EmployeeContract`: one active current contract expected, with history.
- `Person -> AddressOfResidence`: one active current address expected, with history.
- `Person <-> Application`: many-to-many via application items.
- `Application -> ApplicationProgress`: one-to-many ordered history.
- `Person -> Registration`: one active current registration expected, with history.
- `Registration` is application-driven/process-derived (not standalone manual business creation).
- `BorderZonePermit` is a separate business object and is required conditionally by case.

### Person Category Rules
- Family members (`IsEmployee = false`) require passport, visa, registration, and address.
- Family members do not require work permit or employee contract by default.
- Employees (`IsEmployee = true`) require work permit and employee contract, but these can be absent at first-time invitation stage and become mandatory after the relevant issuance/activation milestone.

---

## 3) Core Business Flows (Narrative Level)

Document each flow with:
- Trigger
- Main happy path
- Exceptions
- Completion criteria

Candidate flows for this project:
1. Onboard employee immigration package.
2. Maintain ongoing compliance before expiry windows.
3. Submit and track extension/cancellation/check-in/check-out applications.
4. Resolve rejects/cancellations and re-route work.
5. Close out departures and archive historical records.

---

## 4) Business Object Catalog (What Exists and Why)

Use this as the canonical object worksheet.  
Every object must answer all fields before implementation changes.

| Object | Represents in Real Life | Why Needed (Business Value) | Created When/By | Key Lifecycle Events | If Missing, What Breaks? | Open Questions |
|---|---|---|---|---|---|---|
| `Person` | Foreign employee invited by the company, and also family member(s) of that employee | Root subject for all migration/compliance obligations and process tracking | Created when the company starts managing migration records for an employee/family member | onboarding, arrival, registration, visa/work-permit/passport updates, departure | No reliable person-level ownership of migration states and obligations | `IsEmployee` differentiates employee (`true`) from family member (`false`); confirm if any future person categories are needed |
| `Passport` | Legal travel identity document | Hard precondition for visa/invitation/extension | Recorded at onboarding and renewal | issued, expiring, renewed, archived | Cannot validate eligibility for downstream processes | Required passport validity buffer by process? |
| `Visa` | Legal right to enter/stay | Core residency compliance object | Created on visa issuance/application completion | expiring, extended, cancelled, expired | Cannot determine legal stay status | Are all visa types mapped to different rule sets? |
| `WorkPermitItem` | Legal right to work | Employment compliance control | Issued by authority and recorded by HR | extension required, extended, cancelled, expired | Work legality cannot be assessed | When can visa be valid but permit not required? |
| `AddressOfResidence` | Registration of legal residence | Mandatory local compliance tracking | After arrival/check-in process | check-in, re-registration, expiry, check-out | Cannot prove residence compliance | Different rules by accommodation type? |
| `Registration` | Official registration-domain record for person movement/registration changes | Proof of registration compliance events and updates | Confirmed: used only through registration-related application flows (application-driven) | check-in, internal check-in, registration extension, registration info changes, check-out, internal check-out | No evidence of fulfilled legal duty | Manual registration creation should be blocked by business rule |
| `Application` | Workflow container through authorities | Tracks process state and accountability | When HR starts an authority process | submitted, under review, issued, rejected, cancelled | No process transparency; no SLA tracking | Mandatory attachments/steps per type? |
| `ApplicationProgress` | Time-stamped workflow milestones | Auditability and process visibility | Auto-created on each workflow transition | state/location transitions | No reliable history of decision path | Can transitions be reversed/corrected? |
| `Invitation` | Official invitation authorization for a foreign person | Authorizes visa issuance to passport and enables legal entry/stay process | Before visa issuance process, by visa department/coordinator | submitted, issued, used/expired, cancelled | Visa issuance basis is missing; entry process cannot proceed legally | Which invitation types are still planned vs active? |
| `BorderZonePermit` | Authorization for foreign person to access restricted/border zones | Required for legal movement/activity in border-zone scope | Created when border-zone permit process is initiated/completed | requested, issued, valid, expired, cancelled, archived | Border-zone compliance cannot be tracked or proven | Need exact trigger and process variants by permit type |
| `EmployeeContract` | Employment legal agreement | Needed for employment + extension context | On hiring/renewal | active, expiring, renewed, archived | Context for work-related compliance decisions lost | Is contract validity a hard gate for some apps? |
| `MedicalRecord` | Required health clearance | Gate for specific applications/renewals | After exam issuance | valid, expiring, renewed, archived | Certain submissions must be blocked but cannot be | Exact blocking rules by application type? |
| `TravelHistory` | Actual movement events | Determines in-country obligations | On arrival/departure events | external/internal arrivals and departures | Cannot infer check-in/check-out requirements | Authoritative source and sync timing? |

### Mandatory Object Questions (do not skip)
For each object, answer:
1. Why does this object exist in business language?
2. Who is accountable for creating/updating it?
3. What business event creates it?
4. What legal/operational risk appears if it is wrong?
5. Which other objects depend on it as a prerequisite?
6. Is it source-of-truth or derived/projection data?
7. When is it archived vs deleted?
8. Which fields are mandatory to make decisions?

---

## 5) Relationship Map (Why Relationships Exist)

Use this table to justify relationships by business necessity, not ORM convenience.

| Relationship | Cardinality | Business Rationale | Optional? | Lifecycle Dependency | Validation Questions |
|---|---|---|---|---|---|
| `Person -> Passport` | 1:N over time (confirmed: one active current passport expected) | Person renews passports over years; history is required | At onboarding may be temporarily missing | Visa/invitation/extension checks depend on current passport | Active passport uniqueness must be enforced at business-rule level |
| `Person -> Visa` | 1:N over time (confirmed: one active current visa expected) | Legal stay periods change; must preserve history | Optional before entry | Many compliance states depend on current visa | Active visa uniqueness and "current visa" selection rule must be explicit |
| `Person -> WorkPermitItem` | 1:N over time (confirmed: multiple active work permits can exist at the same time) | Permit renewals/changes require history and parallel active permits may be needed | Optional for some roles | Work legality checks depend on relevant active permit(s) | Need explicit rule for selecting permit(s) in state evaluation/dashboard filters |
| `Person -> Invitation` | 1:N over time (confirmed: multiple active invitations can exist at the same time) | Person may participate in parallel invitation processes/scopes with full history retained | Optional depending on scenario/process | Invitation validity and downstream application eligibility depend on related active invitation(s) | Need explicit rule for invitation selection in forms, states, and reports |
| `Person -> BorderZonePermit` | 1:N over time (confirmed: multiple active permits can exist at the same time) | Person may hold parallel active permits for different border-zone scopes/authorizations | Optional by operational need | Border-zone access/compliance depends on relevant active permit(s) | Need explicit selection and conflict rules across multiple active permits |
| `Person -> MedicalRecord` | 1:N over time (confirmed: one active current medical record expected) | Medical records are renewed over time and history is required | Optional before first issuance | Health-clearance eligibility depends on current active medical record | Need explicit policy for overlap during renewal transitions |
| `Person -> EmployeeContract` | 1:N over time (confirmed: one active current contract expected) | Contract history is required across renewals/changes | Optional for non-employee family members | Employment context and some migration decisions depend on current contract | Need explicit rule that family members do not require employee contract linkage |
| `Person -> AddressOfResidence` | 1:N over time (confirmed: one active current address expected) | Residence changes and re-registration tracking | Optional only when not in country | Registration obligations depend on latest active address | Need explicit rule for internal moves: create new record or update existing |
| `Person -> Application` (via items) | N:N (confirmed: one application can include multiple persons; one person can be in many applications) | Batch processing and shared submissions are operationally necessary | Usually required per process | Progress/alerts rely on this link | Need per-application-type validation for allowed person sets |
| `Application -> ApplicationProgress` | 1:N (confirmed: ordered history is preserved) | Each process has ordered milestones | Not optional | Current state derives from latest progress | Need transition-order validation rules and correction policy |
| `Application -> Invitation` | 1:0..1 by type | Invitation outputs only for invitation-related types | Optional by type | Entry workflow depends on issued invitation | Is Invitation an object or SQL projection in all cases? |
| `Person -> Registration` | 1:N over time (confirmed: one active current registration expected) | Check-in/check-out events accumulate historically | Optional before first arrival | In-country compliance decisions require latest registration | Need explicit model for check-out representation (separate record vs closure event) |

### Relationship Decision Rules
- Use `1:1` only when both objects are inseparable in business meaning.
- Use `1:N` when parent owns repeatable historical facts.
- Use `N:N` only when each side is independently meaningful.
- If no relationship, document why linking would create false coupling.

---

## 6) Business Rules Catalog (Plain Language First)

Write rules in natural language first, then map to implementation.

| Rule ID | Rule (Business Language) | Trigger | Objects Involved | Required System Behavior | Exceptions | Status |
|---|---|---|---|---|---|---|
| BR-001 | Visa/work-permit extension cannot exceed passport validity. | Extension request | Passport, Visa, WorkPermitItem, Application | Block submission and require passport renewal first. | Legal exception process (if any) | Draft |
| BR-002 | Work permit extension window opens 90 days before expiration. | Daily state evaluation | WorkPermitItem, Application | Mark as extension-required and notify coordinator. | Configurable window changes | Draft |
| BR-003 | After visa expiry without extension, check-out must be initiated within legal grace period. | Visa expires | Visa, Person, Registration/Application | Raise critical state and escalation timeline. | Cancellation flow may require immediate action | Draft |
| BR-004 | Arrival requires registration check-in within deadline. | External arrival event | TravelHistory, Person, Application, Registration | Track pending/overdue registration states. | Exempt categories (if any) | Draft |
| BR-005 | Registration is process-derived only (application-driven), not manually created as a standalone business action. | Registration data entry/update attempt | Registration, Application, Person, TravelHistory | Allow registration creation/update only through valid registration application flow and linked movement context. | Authorized data correction policy (if defined later) | Confirmed |
| BR-006 | Family members (`Person.IsEmployee = false`) require passport, visa, registration, and address records; work permit and employee contract are not mandatory. | Person category assignment or related form validation | Person, Passport, Visa, Registration, AddressOfResidence, WorkPermitItem, EmployeeContract | Enforce required/not-required object set based on `IsEmployee` flag. | If family member starts employment, object requirements must be re-evaluated | Confirmed |
| BR-007 | Employees (`Person.IsEmployee = true`) require both work permit and employee contract in v1, but not necessarily at initial first-time invitation stage. These become mandatory after the relevant issuance/activation milestone in the migration process. | Employee onboarding progression and issuance milestones | Person, WorkPermitItem, EmployeeContract, Application | Allow initial invitation/work-permit preparation with empty records for first-time invitees; enforce required records once issuance milestone is reached. | Initial invitation stage for first-time invited employees | Confirmed |
| BR-008 | Border zone permit is optional and required only when a person's planned activity/location requires border-zone access. | Activity/location planning or related application flow | Person, BorderZonePermit, Application | Enforce border-zone permit requirement conditionally by case, not universally for all persons. | None defined yet | Confirmed |
| BR-009 | Applicable objects must be trackable by both process-state and validity-state dimensions concurrently. | State evaluation and dashboard reporting | Visa, WorkPermitItem, Invitation, Application, related process-status views | Show and preserve both dimensions simultaneously (e.g., visa expiring + extension on process) without losing either context. | Object types that inherently have only one state dimension | Confirmed |
| BR-010 | Dashboard must display both validity-state and process-state together for the same business object whenever both are available. | Dashboard rendering/query composition | State dashboard, object/process state sources | Present dual-state context in one visible view/card/row to avoid hidden risk or loss of process context. | Objects with only one applicable state dimension | Confirmed |
| BR-011 | State dashboard is the primary navigation for state management: users should open pre-filtered records by clicking target state entries, without manual filter building. | User selects a state on dashboard | State dashboard, list views, criteria mapping methods | Clicking a state must open the matching record set directly; manual filtering should not be required for core use cases. | Advanced ad-hoc analysis can remain optional | Confirmed |
| BR-012 | Dashboard state click uses strict state matching: clicking a state opens only records in that exact selected state. | User clicks a dashboard state entry | State dashboard, criteria mapping/query logic | Enforce exact-state filter behavior (no automatic grouped/sub-state expansion). | None currently | Confirmed |
| BR-013 | To start invitation process for a future employee, required person-side documents are: valid passport (minimum 6 months validity), education information (diploma copy), photo, medical record, and CV. | Invitation initiation attempt | Person, Passport, Education, MedicalRecord, person profile documents, Invitation/Application | Block invitation start when any required prerequisite document is missing or passport validity is below 6 months. | None defined yet | Confirmed |
| BR-014 | Core migration-service-issued document that enables legal employment is Work Permit (`WorkPermitItem`). | Work eligibility determination | WorkPermitItem, Person, migration process records | Treat issued/valid work permit as primary work-authorization artifact in business decisions and state tracking. | Employment legality still depends on other compliance documents/processes | Confirmed |
| BR-015 | For first-time employee onboarding, invitation and work permit should be applied together in one combined application flow (`App_Inv_And_WP`) as the default path; separate flows are allowed for specific scenarios. | Application planning for future employee entry | Application, ApplicationType, Invitation, WorkPermitItem, Person | Prefer combined application path by default; allow separate application paths when business scenario requires it. | Special-case operational/legal scenarios | Confirmed |
| BR-016 | Work permit is not required when an employee is invited for a period shorter than one month. | Invitation/work-authorization requirement check | Person, Invitation/Application, WorkPermitItem | Skip work-permit requirement for employee invitations with duration < 1 month. | Duration threshold policy changes, if regulation changes | Confirmed |
| BR-017 | Issued valid invitation authorizes visa issuance to the foreigner's passport, and visa can then be issued by Turkmenistan migration service at border customs or by Turkmenistan consulates/embassies abroad. | Invitation issuance and visa issuance planning | Invitation, Visa, Passport, migration/consular authorities | Treat valid invitation as legal basis for subsequent visa issuance channels (border/customs or consular). | None defined yet | Confirmed |
| BR-018 | `ApplicationItem` is the per-person execution unit inside an `Application`: it links one person to one application, carries person-specific context, and tracks person-specific outcomes in multi-person processes. | Application composition and processing | Application, ApplicationItem, Person, person-linked business objects | Use `ApplicationItem` (not only `Application`) to manage person-level status, issued flags, and report/form fields for each included person. | None defined yet | Confirmed |
| BR-019 | `Registration` is used when `Application.ApplicationType.Name` is one of: `App_Reg_Check_In`, `App_Reg_Check_In_Internal`, `App_Reg_Info_Change_Passport`, `App_Reg_Info_Change_Visa`, `App_Reg_Info_Change_Address`, `App_Reg_ext`, `App_Reg_Check_Out`, `App_Reg_Check_Out_Internal`. For non-registration application types, use `ApplicationItem` flow instead. | Application type routing and object usage decision | Application, ApplicationType, Registration, ApplicationItem | Route registration-related application types to `Registration` handling; route all other types through `ApplicationItem` person-level handling. | None defined yet | Confirmed |
| BR-020 | `App_Reg_Check_In_Internal` must follow `App_Reg_Check_Out_Internal` as a mandatory sequence for internal movement registration changes. | Internal movement between regions/locations | Registration, Application, ApplicationType, Person | Enforce sequence: internal check-out first, then internal check-in. | None defined yet | Confirmed |
| BR-021 | `App_Reg_Info_Change_Passport` follows `App_Change_Passport`. `App_Change_Passport` is created when a person renews passport and visa must be moved/transferred from previous passport to new passport. | Passport renewal and passport-change workflow completion | Application, ApplicationType, Passport, Visa, Registration, Person | Allow registration passport-info update flow only after passport-change application context exists. | None defined yet | Confirmed |
| BR-022 | `App_Reg_ext` follows visa extension/prolongation workflows (`App_Visa_Ext`, `App_Visa_Ext_FM`, `App_Visa_Ext_According_to_WP`, `App_Visa_and_WP_Ext`). | Post-visa-extension registration continuation | Application, ApplicationType, Visa, Registration, Person | Create/apply registration extension flow after visa extension process. | None defined yet | Confirmed |
| BR-023 | `App_Reg_Check_Out` is created when visa is expired and person has left country, or when visa is cancelled (cancellation determined by existence of `App_Cancel_Visa`/`App_Cancel_Visa_and_WP` flow). If visa is not expired, `App_Reg_Check_Out` cannot be applied even if person has left country. | Departure and visa-status compliance checks | Application, ApplicationType, Visa, Registration, Person | Gate check-out application by visa-expired or visa-cancellation-application condition. | None defined yet | Confirmed |
| BR-024 | If company needs to check out a person while visa is still valid, visa cancellation flow must be applied first (`App_Cancel_Visa` or `App_Cancel_Visa_and_WP`), then registration check-out proceeds. | Company-initiated early check-out need | Application, Visa, Registration, ApplicationType | Require visa cancellation workflow before check-out when visa has not naturally expired. | None defined yet | Confirmed |
| BR-025 | `App_Reg_Check_Out_Internal` is used for internal departure/deregistration (moving to another internal region/location), followed by internal check-in as required by BR-020. | Internal departure/movement event | Registration, Application, ApplicationType, Person | Record internal check-out and enforce subsequent internal check-in sequence. | None defined yet | Confirmed |
| BR-026 | `App_Reg_Info_Change_Visa` usually follows visa category change application flow (`App_Change_Visa_Category`) to update registration information according to changed visa data. | Visa category change completion and registration data alignment | Application, ApplicationType, Visa, Registration, Person | Use registration visa-info change application after visa-category change workflow where registration data must be updated. | "Usually" indicates possible exceptional operational paths | Confirmed |
| BR-027 | `App_Reg_Info_Change_Address` is created when person changes address of residence to another address within the same city where registration took place. | Same-city residence address change after registration | Application, ApplicationType, AddressOfResidence, Registration, Person | Create registration address-info change application to align registration data with new same-city address. | Cross-city/internal relocation follows internal check-out/check-in flows | Confirmed |
| BR-028 | Employee is considered to have an invitation record when an `Application` of type `App_Inv` or `App_Inv_And_WP` exists and has child `Invitation` with related `InvitationItem` for the target employee. | Invitation presence determination at person level | Application, ApplicationType, Invitation, InvitationItem, Person | Determine "has invitation" based on existence of application-linked invitation structure for that employee. | None defined yet | Confirmed |
| BR-029 | Invitation is considered `Expired` when `Today >= Invitation.ExpirationDate` and the related `InvitationItem` is neither (a) in invitation cancellation/change workflows (`App_Cancel_Inv`, `App_Change_Inv`, `App_Cancel_Inv_WP`) nor (b) already used by being linked through `Visa.InvitationItem`. | Invitation expiry evaluation | Invitation, InvitationItem, Application, ApplicationType, Visa | Mark invitation as expired only when date condition is met and exclusion conditions (cancellation/change in progress or already used for visa issuance) do not apply. | None defined yet | Confirmed |
| BR-030 | Visa cancellation decision must be determined by cancellation application existence (`App_Cancel_Visa` or `App_Cancel_Visa_and_WP`) rather than relying only on `Visa.IsCancelled` flag. | Visa cancellation state determination | Visa, Application, ApplicationType, ApplicationItem | Treat cancellation application flow as primary cancellation evidence in business logic and state decisions. | None defined yet | Confirmed |
| BR-031 | Whether a specific `Visa` is included in a visa-extension flow must be determined via explicit link `ApplicationItem.CurrentVisa -> Visa` (or inverse `Visa.AssociatedApplicationItems`) with extension application types. | Visa extension inclusion determination | Visa, ApplicationItem, Application, ApplicationType | Use relational linkage (not inference) to decide if visa is on extension/in extension flow. | None defined yet | Confirmed |
| BR-032 | Whether extension is not required for a specific `Visa` is determined by explicit business flag `Visa.ExtensionRequired = false`. | Visa extension-necessity determination | Visa | Use `ExtensionRequired` as authoritative indicator for "extension not required" scenarios (e.g., leaving employee, contract end) instead of inferring from expiry dates alone. | None defined yet | Confirmed |
| BR-033 | Visa extension process should be started when visa enters extension window (90 days before `Visa.ExpirationDate`), provided extension is required and passport-validity preconditions are satisfied. | Extension-start timing check | Visa, Passport, Application, ApplicationType | Trigger/start visa extension workflow at 90-day window (`ExtensionApplicationRequired`) when `ExtensionRequired = true` and extension is legally feasible. | None defined yet | Confirmed |
| BR-034 | Visa should be considered `Active` as long as it is not cancelled and not expired (and record is active), including cases where it is expiring-soon or currently on extension process. | Visa active-state determination | Visa, Application/ApplicationType (for cancellation and extension-flow evidence) | Treat expiring and on-extension as sub-states/dimensions within active visa population; do not classify them as non-active solely due to warning window or extension in progress. | None defined yet | Confirmed |
| BR-035 | For determining whether a visa extension flow is completed/prolonged, issued-visa evidence is resolved via `Visa.IssuingApplicationItem`, where `IssuingApplicationItem` is the same `ApplicationItem` instance that contains `CurrentVisa` (visa being extended) and whose parent application is an extension type. Process outcome code is secondary and may lag linkage updates. | Extension-completion determination and data consistency handling | Visa, ApplicationItem, Application, ApplicationType | Determine completion from `IssuingApplicationItem` linkage on the extension `ApplicationItem` context; treat process outcome code as secondary signal. | None defined yet | Confirmed |
| BR-036 | Visa is considered expired when `Today >= Visa.ExpirationDate`, provided higher-priority conditions do not classify it as cancelled (via cancellation application evidence) or already completed/prolonged via extension-flow issued-visa evidence (per BR-035). | Visa expiry-state determination | Visa, Application, ApplicationType, ApplicationItem | Evaluate expiry with precedence-aware logic: cancellation and extension-completion evidence can override raw date-only interpretation. | None defined yet | Confirmed |
| BR-037 | `ApplicationProgress` is the ordered process-event history for an `Application`: it records workflow movement/decisions by authority stages, provides audit trail, and determines current process state from the latest entry. | Application workflow transition events | Application, ApplicationProgress, ApplicationState, authority/location context | Persist each process transition as history; derive "current process state" from latest progress record for dashboard and operational tracking. | None defined yet | Confirmed |
| BR-038 | Current application state is always determined by the latest `ApplicationProgress` entry by date/time. | Current-state resolution during workflow tracking | Application, ApplicationProgress | Resolve current state using most recent timestamped progress entry (date/time-based precedence). | None defined yet | Confirmed |
| BR-039 | A person can have at most one active valid visa at a time (or zero when no current valid visa exists). | Visa cardinality and current-visa selection | Person, Visa | Enforce single active valid visa per person for business consistency and deterministic state evaluation. | None defined yet | Confirmed |
| BR-040 | Main participants for determining `Visa` states are: `Visa` itself, linked `ApplicationItem`, parent `Application`, `ApplicationType`, `ApplicationProgress`, `ApplicationStatus`, another related `Visa` record (for extension/change/transfer-to-new-passport scenarios), and `Registration` with its parent `Application`/`ApplicationType` context. These participants cover the vast majority of visa-state determination logic. | Visa state determination and rule evaluation | Visa, ApplicationItem, Application, ApplicationType, ApplicationProgress, ApplicationStatus, related Visa records, Registration | Use this participant set as the primary data graph for visa-state evaluation (including process, validity, transfer/change, and registration-related dependencies). | Residual edge cases may require additional context outside this core set | Confirmed |
| BR-041 | `Visa` to `Application` linkage can be established through two BO paths depending on process domain: (1) `ApplicationItem` path (`ApplicationItem.CurrentVisa -> Visa`, `ApplicationItem.Application -> Application`) and (2) `Registration` path (`Registration.CurrentVisa -> Visa`, `Registration.Application -> Application`) for registration-related contexts. | Visa-application relation resolution | Visa, ApplicationItem, Registration, Application | Resolve link source by process type: general/visa-process flows use `ApplicationItem`, registration flows use `Registration`. | None defined yet | Confirmed |
| BR-042 | `Visa.Active` state definition (confirmed): visa is considered active when not cancelled, not expired, and record is active. Active population may still include visas in expiring-soon window and visas on extension process; these are tracked as additional dimensions/sub-states rather than non-active classification. | Visa active-state semantics | Visa, Application/ApplicationType (for cancellation evidence), extension-flow linkage | Keep active-state semantics broad enough to include warning/process sub-states while preserving cancellation/expiry precedence. | None defined yet | Confirmed |
| BR-043 | `Visa.Cancelled` state definition (confirmed): visa is considered cancelled when cancellation application evidence exists (`App_Cancel_Visa` or `App_Cancel_Visa_and_WP`) for the linked visa context. Linkage to application context must be resolved through `ApplicationItem` path (`CurrentVisa`) and/or `Registration` path where applicable. `Visa.IsCancelled` flag alone is not sufficient as primary evidence. | Visa cancelled-state semantics | Visa, Application, ApplicationType, ApplicationItem, Registration | Determine cancelled state from linked cancellation workflow evidence via explicit visa-application linkage paths. | None defined yet | Confirmed |
| BR-044 | `Visa.OnExtension` state definition (confirmed): visa is considered on extension when it is linked to an extension application (`App_Visa_Ext`, `App_Visa_Ext_FM`, `App_Visa_Ext_According_to_WP`, `App_Visa_and_WP_Ext`) through `ApplicationItem.CurrentVisa` linkage and completion evidence via `IssuingApplicationItem` is not yet present for that same extension `ApplicationItem` context. | Visa extension-in-progress semantics | Visa, ApplicationItem, Application, ApplicationType | Determine on-extension state from explicit visa-extension linkage while excluding flows already completed by `IssuingApplicationItem` evidence on the linked extension item. | None defined yet | Confirmed |
| BR-045 | `Visa.Expired` state definition (confirmed): visa is expired when `Today >= Visa.ExpirationDate` and it is not already classified as `Cancelled` by cancellation-application evidence and not already classified as `Extended/Prolonged` by extension-completion evidence. | Visa expired-state semantics | Visa, Application, ApplicationType, ApplicationItem, Registration | Apply precedence-aware expiry logic so cancellation/extension-completion classifications override date-only expiry interpretation. | None defined yet | Confirmed |
| BR-046 | `Visa.ExpiringSoon` membership is determined by entering warning window before expiration and does not require `ExtensionRequired = true`. `ExtensionRequired` only influences subtype/next-action semantics (for example "not required" handling), not whether visa belongs to warning-window population itself. | Visa expiring-soon semantics | Visa, SystemSettings thresholds, extension-required flag | Keep warning-window detection independent from extension-required business decision; use `ExtensionRequired` only for branching actions/substates. | None defined yet | Confirmed |
| BR-047 | `Visa.ExpiringSoon` can coexist with `Visa.OnExtension`: entering extension process does not remove visa from warning-window membership until completion/override conditions apply. | Dual-state coexistence semantics for visa warning/process dimensions | Visa, ApplicationItem, Application, ApplicationType, SystemSettings thresholds | Preserve both dimensions simultaneously (warning + process) while extension is in progress. | None defined yet | Confirmed |
| BR-048 | `Visa.ExpiringSoonNotRequired` (or equivalent warning-no-extension-needed substate) applies when visa is in warning window and `ExtensionRequired = false`, while still not cancelled and not expired. This remains part of active population semantics. | Visa warning substate when extension is not required | Visa, SystemSettings thresholds, extension-required flag, cancellation/expiry precedence evidence | Distinguish warning-window visas that do not require extension from those that require extension, without removing active status dimension. | None defined yet | Confirmed |
| BR-049 | BOs (including `Visa`) can hold multiple concurrent states across different dimensions (for example validity, process, and process-substage). Example: a visa may be `ExpiringSoon`, `OnExtension`, and `AtMinistry1` at the same time. | Multi-dimension state resolution and display | Visa, Application, ApplicationProgress, ApplicationType, state dashboard | Preserve and display all applicable state dimensions concurrently instead of forcing a single-state label model. | Dimension-concurrency may be limited by terminal/superseded states such as `Changed` (see BR-050) | Confirmed |
| BR-050 | `Visa.Changed` means the visa has been replaced by another visa and is no longer valid as the active legal basis. Therefore, expiry concern tracking for the replaced visa should not be a decision driver. | Visa changed/superseded-state semantics | Visa, related/new Visa, Application/ApplicationType linkage | Treat changed visa as superseded/inactive-for-validity decisions; focus expiry/compliance concern on the replacing active visa instead. | None defined yet | Confirmed |
| BR-051 | `NoVisa` (person-level visa absence state) applies when a person has no active current valid visa record (no visa created, or all visa records are superseded/expired/cancelled). | Person-level visa availability determination | Person, Visa | Classify as `NoVisa` when no current valid visa exists; trigger investigation when person is expected to be in-country. | None defined yet | Confirmed |
| BR-052 | `Visa.Archived` means visa record is historical/inactive (`IsActive = false`) and is not used as current legal basis. Archived records remain for history/audit and should not drive active compliance alerts. | Visa archived-state semantics | Visa | Keep archived visas visible for audit/history but exclude them from active compliance-driving state counts/actions. | None defined yet | Confirmed |
| BR-053 | `Visa.AtMinistry1` / `SubmittedToMinistry1` applies when the extension-linked application's latest progress state is first-ministry review started (`1_REVIEW_STARTED`), with visa linkage resolved via `ApplicationItem.CurrentVisa`. This process substage can coexist with validity/extension dimensions. | Visa extension process substage semantics | Visa, ApplicationItem, Application, ApplicationProgress, ApplicationType | Determine first-ministry substage from latest linked process progress while preserving concurrent validity/process dimensions. | None defined yet | Confirmed |
| BR-054 | `Visa.AtMinistry2` / `SubmittedToMinistry2` applies when the extension-linked application's latest progress state is second-ministry review started (`2_REVIEW_STARTED`), with visa linkage resolved via `ApplicationItem.CurrentVisa`. This process substage can coexist with validity/extension dimensions. | Visa extension process substage semantics | Visa, ApplicationItem, Application, ApplicationProgress, ApplicationType | Determine second-ministry substage from latest linked process progress while preserving concurrent validity/process dimensions. | None defined yet | Confirmed |
| BR-055 | `Visa.Ministry1Approved` applies when latest extension-linked progress state is `1_REVIEW_APPROVED`, indicating first-ministry approval and readiness for next process stage. This state can coexist with validity/extension dimensions. | Visa extension approval substage semantics | Visa, ApplicationItem, Application, ApplicationProgress, ApplicationType | Resolve first-ministry approval from latest linked process progress while preserving concurrent dimensions. | None defined yet | Confirmed |
| BR-056 | `Visa.Ministry2Approved` applies when latest extension-linked progress state is `2_REVIEW_APPROVED`, indicating ministry approvals are complete and case is ready for processing/issuance stage. This state can coexist with validity/extension dimensions. | Visa extension approval substage semantics | Visa, ApplicationItem, Application, ApplicationProgress, ApplicationType | Resolve second-ministry approval from latest linked process progress while preserving concurrent dimensions. | None defined yet | Confirmed |
| BR-057 | `Visa.Ministry1Rejected` applies when latest extension-linked progress state is `1_REVIEW_REJECTED`, meaning extension flow was rejected at first-ministry stage. This rejection process state can coexist with validity dimension but blocks normal extension-success path until corrective action/reapplication. | Visa extension rejection substage semantics | Visa, ApplicationItem, Application, ApplicationProgress, ApplicationType | Resolve first-ministry rejection from latest linked process progress and treat as blocking process-state outcome for current extension path. | None defined yet | Confirmed |
| BR-058 | `Visa.Ministry2Rejected` applies when latest extension-linked progress state is `2_REVIEW_REJECTED`, meaning extension passed first ministry but was rejected at second-ministry stage. This rejection process state can coexist with validity dimension but blocks extension-success path until corrective action/reapplication. | Visa extension rejection substage semantics | Visa, ApplicationItem, Application, ApplicationProgress, ApplicationType | Resolve second-ministry rejection from latest linked process progress and treat as blocking process-state outcome for current extension path. | None defined yet | Confirmed |
| BR-059 | `Visa.ProcessStarted` (`PROCESS_STARTED`) applies when latest extension-linked process state is `PROCESS_STARTED`, meaning processing stage has begun after review approvals. This process state can coexist with validity and extension dimensions. | Visa extension processing-stage semantics | Visa, ApplicationItem, Application, ApplicationProgress, ApplicationType | Resolve processing-started state from latest linked process progress while preserving concurrent dimensions. | None defined yet | Confirmed |
| BR-060 | Dedicated extension-cancellation branch is required: when `App_Cancel_Visa_Ext` or `App_Cancel_Visa_and_WP_Ext` exists for a visa context and is in progress, visa should enter `ExtensionCancellationInProgress` (or equivalent) process state, distinct from general `PROCESS_CANCELLED` and distinct from full visa cancellation flows (`App_Cancel_Visa`, `App_Cancel_Visa_and_WP`). | Visa extension-cancellation branch semantics | Visa, ApplicationItem, Application, ApplicationType, ApplicationProgress | Track company-initiated extension cancellation as its own process-state dimension to preserve business intent and routing decisions. | None defined yet | Confirmed |
| BR-061 | When extension-cancellation flow is approved/completed, visa should transition to a dedicated terminal `ExtensionCancelled` (or equivalent) state, not back to normal extension path states. | Extension-cancellation terminal outcome semantics | Visa, ApplicationItem, Application, ApplicationType, ApplicationProgress | Use separate terminal state to preserve explicit business outcome that extension path was intentionally cancelled. | None defined yet | Confirmed |
| BR-062 | Rejected extension-cancellation flow is currently not a supported/considered scenario in this business model. | Scope boundary for extension-cancellation outcomes | Visa, Application, ApplicationType | Treat extension-cancellation rejection handling as out-of-scope until business introduces that scenario explicitly. | None defined yet | Confirmed |
| BR-063 | Rejection should be modeled at two levels: application-level (`Application.CurrentState = PROCESS_REJECTED`) and person-level (`Rejection` + `RejectionItem`). Person-specific state decisions (including visa/person outcomes) must use `RejectionItem` evidence rather than assuming all application participants are equally rejected. | Rejection granularity and person-level decision semantics | Application, Rejection, RejectionItem, ApplicationItem, Visa, Person | Preserve process-level visibility while enabling accurate per-person rejection outcomes and actions. | None defined yet | Confirmed |
| BR-064 | For visa/person rejected-state classification, matching person-level `RejectionItem` evidence is mandatory. Application-level `PROCESS_REJECTED` alone is not sufficient to mark a specific visa/person as rejected. | Visa/person rejected-state determination | Visa, Person, Application, Rejection, RejectionItem, ApplicationItem | Require person-context `RejectionItem` linkage as a must-have criterion for rejected outcome semantics at visa/person level. | None defined yet | Confirmed |
| BR-065 | `Application.CurrentState = PROCESS_REJECTED` is optional (not mandatory). If this state is used, it is valid only when all people included in that application are rejected (each with corresponding `RejectionItem` evidence). Even in full-population rejection cases, `Rejection` and `RejectionItem` records must still be created regardless of whether `PROCESS_REJECTED` is set. | Application-level rejected-state validity rule | Application, ApplicationItem, Rejection, RejectionItem, Person | Treat `PROCESS_REJECTED` as optional status label; enforce evidence integrity through mandatory rejection artifacts and per-person rejection records. | None defined yet | Confirmed |

---

## 7) Lifecycle and State Transition Definitions

For each critical object, define:
- States (business meaning)
- Entry/exit criteria
- Priority when multiple states could apply
- Actions/owners
- Dual-state requirement (confirmed): where applicable, objects must support both validity-state and process-state tracking at the same time (example: Visa can be `ExpiringSoon` while related extension application is `OnProcess`).

Reference existing state specs while refining business wording:
- `docs/BO_STATE_TRACKING.md`
- `docs/STATE_SPECIFICATIONS.md`

---

## 8) Traceability to Code (Business -> Implementation)

This is the anti-gap section.  
Each important business rule should map to concrete code artifacts.

| Business Rule ID | Business Object(s) | Service/Evaluator/Controller | UI Surface | Data Source | Test Coverage |
|---|---|---|---|---|---|
| BR-001 | Passport, Visa, WorkPermitItem | `*StateEvaluator`, submission validation services | State dashboard + application forms | BO + SQL views | Unit + integration + dashboard scenario |
| BR-002 | WorkPermitItem, Application | `WorkPermitItemStateEvaluator` + scheduling logic | Dashboard, notifications | BO + app progress | Scenario + evaluator tests |
| BR-003 | Visa, Registration/Application | Visa/registration state evaluators and SQL status views | Dashboard critical states | BO + SQL views | E2E compliance flow |

---

## 9) Pending Decisions and Open Questions

### Pending Decisions (High Priority)
- Direct non-primary web app users are still TBD.
- Define measurable KPI targets (numeric thresholds) for the first 3 months.
- Confirm object-level rule for selecting the "relevant active" item when multiple active records exist (`WorkPermitItem`, `Invitation`, `BorderZonePermit`).
- Define transition correction policy when process milestones are entered out of sequence.
- Define explicit check-out representation model in `Registration` (new record vs closure pattern), aligned with existing implementation.
- Define data-correction exception process for BR-005 (who can correct, what is audited).

### Additional Open Questions
- Which compliance failures are most expensive today (penalty, delay, reputation)?
- What SLA is expected from "state turns warning/critical" to "action taken"?
- Which outcomes matter most in first release: fewer breaches, faster processing, fewer rejects?
- Which object is authoritative when records conflict (BO record vs authority process result)?
- Which objects are immutable historical facts vs editable operational records?
- Which objects are legal evidence and must never be hard-deleted?
- Which deadlines are legal constants vs configurable settings?
- Which rules block submission vs only warn/escalate?
- Who can override a blocked rule, and what audit trail is mandatory?

### Decision Status Convention
- `Confirmed`: explicitly validated with stakeholder.
- `Draft`: proposed baseline rule, pending explicit confirmation.
- `Pending`: unresolved item requiring a decision.

---

## 10) Review and Sign-Off Checklist

- Problem statement approved by business owner.
- Each business object has complete "why/when/who/risk" answers.
- Each relationship has explicit cardinality rationale.
- Top 10 business rules documented in plain language.
- Rule-to-code traceability matrix created for those top rules.
- Conflicting assumptions resolved or explicitly marked as open risks.

---

## 11) Suggested Working Process

1. Run a 60-90 minute workshop using Sections 1, 4, 5, and 9.
2. Freeze terminology (ubiquitous language) after workshop.
3. Approve top rules in Section 6 before coding new logic.
4. Implement with Section 8 traceability updated in the same PR.
5. Keep this doc versioned; every logic change updates this baseline first.

