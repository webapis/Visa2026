# Prompt: Update application type readiness (Ready / Pending / Not ready)

> Paste the **Task prompt** below into a new Cursor (or other AI) chat when you want to change rollout status for application types.
> Fill in the bracketed fields. Status is **code-only** — nothing is written to the database.

---

## What these states mean

| State | Picker | Meaning | Who moves it here |
|-------|--------|---------|-------------------|
| **Ready** ✓ | Selectable | Stakeholder **approved** — production use | Developer, after stakeholder sign-off |
| **Pending** ◐ | Selectable | Developer **finished implementation**; awaiting stakeholder approval — **users can test** on real Application forms | Developer, when feature is ready for review |
| **Not ready** ✗ | Visible, **not** selectable | Implementation **not complete** | Default for all types not listed in Ready/Pending sets |

**Not in database:** only `ApplicationType.SelectionCode` (ministry 3-digit code) is stored in SQL. Readiness is a temporary map for developers and stakeholders during rollout.

---

## File the agent must edit

| File | Purpose |
|------|---------|
| `Visa2026.Module/Services/ApplicationTypeDevelopmentReadiness.cs` | **Only** file to change for readiness |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeSelectionCodeSeed.cs` | Reference for `Name` ↔ `SelectionCode` (do **not** edit for readiness) |

### Four hash sets (mutually exclusive per type)

| Set | Use when state is |
|-----|-------------------|
| `ReadyByName` / `ReadyBySelectionCode` | **Ready** |
| `PendingByName` / `PendingBySelectionCode` | **Pending** |
| *(not in any set)* | **Not ready** |

**Rules for the agent:**

1. Each `ApplicationType.Name` (and/or its `SelectionCode`) must appear in **at most one** of Ready or Pending — never both.
2. When moving a type to a new state, **remove** it from the other set(s) first.
3. Prefer adding **`ApplicationType.Name`** (e.g. `App_Inv`). Add **`SelectionCode`** only when you need a code-only rule or the name is unknown.
4. Do **not** add DB columns, BO properties, seeds, or migrations for readiness.
5. Do **not** change picker/controller logic unless the user explicitly asks.
6. After edits, run `dotnet build Visa2026.Module/Visa2026.Module.csproj -c Debug`.

---

## Name ↔ code reference (ministry seed)

Use exact `Name` values from `ApplicationTypeSelectionCodeSeed.cs`:

| Name | Code |
|------|------|
| App_Inv | 101 |
| App_Inv_FM | 102 |
| App_Inv_According_to_WP | 103 |
| App_Change_Inv | 104 |
| App_Inv_And_WP | 105 |
| App_Sevice_Passport | 201 |
| App_Reg_Check_In | 301 |
| App_Reg_Check_In_Internal | 302 |
| App_Reg_Info_Change_Passport | 303 |
| App_Reg_Info_Change_Visa | 304 |
| App_Reg_Info_Change_Address | 305 |
| App_Reg_ext | 306 |
| App_Reg_Check_Out | 307 |
| App_Reg_Check_Out_Internal | 308 |
| App_WP_Ext | 401 |
| App_Additional_WP_location | 402 |
| App_Business_Trip_Departure | 501 |
| App_Business_Trip_Arrival | 502 |
| App_Border_Zone_Permission | 601 |
| App_Visa_Ext_According_to_WP | 701 |
| App_Visa_Ext | 702 |
| App_Exit_Visa | 703 |
| App_Change_Visa_Category | 704 |
| App_Change_Passport | 705 |
| App_Visa_Ext_FM | 706 |
| App_Visa_For_New_Born_FM | 707 |
| App_Visa_and_WP_Ext | 708 |
| App_Cancel_BZ | 801 |
| App_Cancel_App | 802 |
| App_Cancel_Visa_and_WP_Ext | 803 |
| App_Cancel_Visa_Ext | 804 |
| App_Cancel_Inv | 805 |
| App_Cancel_Inv_WP | 806 |
| App_Cancel_Visa | 807 |
| App_Cancel_Visa_and_WP | 808 |
| App_Cancell_WP | 809 |

---

## Task prompt (copy from here)

```
I am working on Visa2026 (.NET 8, XAF Blazor). Update application type **development readiness** only.

Read and follow:
- Visa2026.Module/Services/PROMPT_APPLICATION_TYPE_READINESS.md (state definitions and rules)
- Visa2026.Module/Services/ApplicationTypeDevelopmentReadiness.cs (file to edit)

State meanings:
- **Ready** — stakeholder approved; selectable
- **Pending** — developer implementation complete; awaiting stakeholder approval; **selectable for testing**
- **Not ready** — implementation not complete; **not** selectable (remove from Ready and Pending sets)

Do NOT persist readiness to the database. Edit only ApplicationTypeDevelopmentReadiness.cs.

## Changes requested

[Describe what to change — examples below]

### Example A — implementation finished, send for stakeholder review (→ Pending)
Move to **Pending**:
- App_Reg_Check_Out (307)

Remove from Ready if present. Do not add to Ready.

### Example B — stakeholder approved (→ Ready)
Move to **Ready**:
- App_Inv_FM (102)

Remove from Pending. Add to ReadyByName and/or ReadyBySelectionCode.

### Example C — still in development (→ Not ready)
Set to **Not ready** (remove from Ready and Pending):
- App_Visa_Ext (702)

### Example D — bulk replace Pending list
Replace entire Pending set with only: App_Inv_FM, App_Visa_and_WP_Ext
Keep Ready set unchanged unless I listed changes above.

After editing, build Visa2026.Module and summarize which names/codes moved and their new state.
```

---

## Short prompts (one-liners)

**Mark Pending (ready for stakeholder review):**
```
Update ApplicationTypeDevelopmentReadiness: set Pending for App_[Name] ([code]). Remove from Ready if listed there. Build Module only.
```

**Mark Ready (stakeholder approved):**
```
Update ApplicationTypeDevelopmentReadiness: set Ready for App_[Name] ([code]). Remove from Pending. Build Module only.
```

**Mark Not ready (implementation incomplete):**
```
Update ApplicationTypeDevelopmentReadiness: set Not ready for App_[Name] ([code]) — remove from Ready and Pending. Build Module only.
```

**Show current map:**
```
List all application types in ApplicationTypeDevelopmentReadiness with state Ready, Pending, or Not ready (use ApplicationTypeSelectionCodeSeed for codes). Do not change files.
```

---

## Manual check after deploy

1. Open Application detail → **…** type code picker.
2. **Ready** and **Pending** rows: ✓ / ◐, clickable.
3. **Not ready**: ✗, muted, not clickable.
4. Typing a **Not ready** 3-digit code shows a warning and does not set Application type.

---

## Related docs

- `docs/APPLICATION_BO_TYPE_SELECTION_REFACTOR.md` — quick code refactor and picker UX
- `Visa2026.Module/Services/ApplicationTypeReadinessStatus.cs` — enum (do not extend without team agreement)
