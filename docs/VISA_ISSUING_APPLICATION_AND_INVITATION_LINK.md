# Visa ↔ Issuing Application Item ↔ Invitation Item linkage

**Status:** Path A implemented. Path B hybrid (§4.1) decided — ProcessNumber/sibling for IssuingApplicationItem; target closest-match for InvitationItem.

**Related BOs:** `Visa`, `Invitation`, `InvitationItem`, `Application`, `ApplicationItem`  
**Related capabilities:** `ApplicationType.CanIssueVisa`, `ApplicationType.CanIssueInvitation`  
**Import skill:** `.cursor/skills/visa2014-to-visa2026-import`

---

## 1. Purpose

The system must track which `InvitationItem` of an `Invitation` has been **used**.

**Used** means: a `Visa` was issued as a result of that invitation line.

When a new `Visa` is created, code must identify and set:

| Visa property | Related source BO | When |
|---------------|-------------------|------|
| `IssuingApplicationItem` | `ApplicationItem` | Parent application type has **`CanIssueVisa` or `CanIssueInvitation`** |
| `InvitationItem` | `InvitationItem` | Parent / issuing application type has **`CanIssueInvitation`** |

Those properties remain **read-only** in the UI for manual create (officers can **see** the values; they cannot edit them).

---

## 1.1 Two separate matching logics (mandatory split)

Identifying the related source `ApplicationItem` and (when applicable) `InvitationItem` for a `Visa` requires **two independent implementations**. Do **not** share one matcher for both paths.

| Path | Trigger | Home | Matcher goal |
|------|---------|------|--------------|
| **A — Manual create** | Officer creates a new `Visa` in the XAF UI | `Visa2026.Module` (domain / controllers / helpers) | Strict business rules: auto-default + validation (§3) |
| **B — Legacy import** | `--import-visa2014` creates / maps `Visa` | `Visa2026.DataImporter` + visa2014-to-visa2026-import skill | Closest-match against already-imported sources (§4) |

```text
                    ┌─────────────────────────────────────┐
                    │  Shared eligibility (capabilities)  │
                    │  IssuingApplicationItem ← AppItem   │
                    │    if CanIssueVisa ∨ CanIssueInvitation
                    │  InvitationItem ← InvitationItem    │
                    │    if CanIssueInvitation            │
                    └──────────────┬──────────────────────┘
                                   │
              ┌────────────────────┴────────────────────┐
              ▼                                         ▼
   ┌──────────────────────┐               ┌──────────────────────────┐
   │ Path A: Manual create│               │ Path B: Legacy import    │
   │ §3 matching logic    │               │ §4 matching logic        │
   │ (UI / Module)        │               │ (DataImporter)           │
   └──────────────────────┘               └──────────────────────────┘
```

**Hard separation rules:**

1. Manual-create matcher (§3) must **not** run during import.
2. Import matcher (§4) must **not** be used for UI auto-default.
3. Each path resolves **both** targets with path-specific steps:
   - match `ApplicationItem` → set `Visa.IssuingApplicationItem`
   - if `CanIssueInvitation` → match `InvitationItem` → set `Visa.InvitationItem`
4. Shared concepts (capability flags, cancelled exclusion, single-use intent) may be documented once (§2) but algorithms stay separate.

---

## 2. Shared definitions and eligibility

| Term | Meaning |
|------|---------|
| **Used invitation item** | An `InvitationItem` that is linked from a `Visa.InvitationItem` because that visa was issued from that invitation line. |
| **Issuing application item** | `Visa.IssuingApplicationItem` — the matched `ApplicationItem` under an eligible issuing application for that person. |
| **Eligible issuing application types** | Application types with **`CanIssueVisa = true` OR `CanIssueInvitation = true`**. Either flag qualifies a type as a valid parent for `IssuingApplicationItem`. |
| **Visa-issuing application types** | Application types with `CanIssueVisa = true`. Included in eligible issuing types. |
| **Invitation-issuing application types** | Application types with `CanIssueInvitation = true`. Included in eligible issuing types; when the selected issuing application’s type has this flag, `Visa.InvitationItem` **must** also be set. |
| **Path A / Manual create** | Officer creates a new `Visa` in the XAF UI — uses §3 matcher. |
| **Path B / Import create** | `Visa` rows created by VISA2014 → Visa2026 import — uses §4 matcher. |

### 2.1 Eligible issuing application types (`CanIssueVisa` **or** `CanIssueInvitation`)

Valid parents for `Visa.IssuingApplicationItem` are types where **`CanIssueVisa || CanIssueInvitation`**. Catalog today (union of both flags; invitation-issuing rows are included explicitly via `CanIssueInvitation`, not only as a side effect of `CanIssueVisa`):

| Code | English name | Also `CanIssueInvitation`? |
|------|----------------|----------------------------|
| `App_Inv` | Obtain invitation | Yes |
| `App_Inv_FM` | Obtain invitation (family member) | Yes |
| `App_Inv_According_to_WP` | Obtain invitation per work permit | Yes |
| `App_Inv_And_WP` | Obtain invitation and work permit | Yes |
| `App_Change_Inv` | Change invitation | Yes |
| `App_Visa_Ext` | Extend visa validity | No |
| `App_Visa_Ext_FM` | Extend visa validity (family member) | No |
| `App_Visa_Ext_According_to_WP` | Extend visa per work permit | No |
| `App_Visa_and_WP_Ext` | Extend visa and work permit | No |
| `App_Exit_Visa` | Exit visa | No |
| `App_Change_Visa_Category` | Change visa category | No |
| `App_Change_Passport` | Transfer visa to new passport | No |
| `App_Visa_For_New_Born_FM` | Visa for newborn (family member) | No |

Cancel types (`App_Cancel_Visa`, `App_Cancel_Inv`, etc.) have both flags **false** and are never candidates.

> **Rule:** `IssuingApplicationItem` eligibility = **`CanIssueVisa` ∪ `CanIssueInvitation`**. Do not treat invitation-issuing types as optional extras outside the visa-issuing list — they are first-class eligible types. If a future catalog row has only `CanIssueInvitation` (without `CanIssueVisa`), it remains a valid `IssuingApplicationItem` candidate.

### 2.2 When each related source must be identified

Applies to **both** Path A and Path B (eligibility only; matching algorithm differs per path):

| Target property | Source BO to match | Condition |
|-----------------|--------------------|-----------|
| `Visa.IssuingApplicationItem` | `ApplicationItem` | Application type has `CanIssueVisa` **or** `CanIssueInvitation` |
| `Visa.InvitationItem` | `InvitationItem` | Issuing application type has `CanIssueInvitation` |
| `Visa.InvitationItem` | — (leave null) | Issuing application type is `CanIssueVisa` only |

---

## 3. Path A — Manual Visa create (UI / Module)

**Scope:** Only when the user manually creates a new `Visa`.  
**Implementation home:** `Visa2026.Module` (not DataImporter).  
**Must be enforced** (validation + auto-default), not merely suggested.

### 3.0 Path A matching responsibilities

Path A owns its own matcher that, for a new UI `Visa`:

1. **Match `ApplicationItem`** among eligible types (`CanIssueVisa` ∨ `CanIssueInvitation`) → set `IssuingApplicationItem`.
2. **If** that application’s type has `CanIssueInvitation` → **separately match `InvitationItem`** → set `InvitationItem`.
3. If not invitation-issuing → leave `InvitationItem` null.

Path A matching steps below apply only to this path. Path B must not call this logic.

### 3.1 Single-use linkage (1:1)

- An `IssuingApplicationItem` may be linked to **at most one** `Visa`.
- An `InvitationItem` may be linked to **at most one** `Visa`.
- Once linked to a visa, that application item / invitation item **cannot** be linked to another visa (invitation line is single-use for visa issuance).

### 3.2 Chronology

Strict later-than chain (all comparisons use calendar dates):

1. When `InvitationItem` is set: `Visa.IssueDate` **>** `Invitation.IssuedDate`  
2. Always (when issuing app is set): `Invitation.IssuedDate` **>** `IssuingApplicationItem.Application.Date` when invitation is linked; otherwise `Visa.IssueDate` **>** `IssuingApplicationItem.Application.Date`

Product chain: **visa issued later than invitation formalization, which is later than application date.**

> Resolved as strict `>` (not `≥`). Chronology vs invitation date applies only when `InvitationItem` is set (§2.2).

### 3.3 Match ApplicationItem (IssuingApplicationItem)

Only application items whose parent `Application.ApplicationType` has **`CanIssueVisa = true` or `CanIssueInvitation = true`** are valid candidates for `Visa.IssuingApplicationItem` (see §2.1).

### 3.3a Match InvitationItem (only if CanIssueInvitation)

If the matched `IssuingApplicationItem` belongs to an application whose type has `CanIssueInvitation = true`, then Path A must also match and set `Visa.InvitationItem` (same person, matching invitation under that application / invitation flow, unused, not cancelled — per §3.1, §3.4).

If the issuing type does **not** have `CanIssueInvitation`, `InvitationItem` must remain null and must not be required.

### 3.4 Cancelled / ineligible sources excluded

Candidates for `IssuingApplicationItem` / `InvitationItem` must be excluded when any of the following apply:

- `InvitationItem.IsCancelled` (and, when matching invitation lines, also exclude `IsChanged` and already `IsUsed`)
- Application-item / workflow cancel flags on the related `ApplicationItem` where present
- Parent `Application` is process-cancelled (latest `ApplicationProgress` terminal cancelled state)
- Invitation header / item cancelled state as applicable to the candidate row

Cancelled (and changed / already-used invitation lines) are never defaults for Path A.

### 3.5 Default selection: predecessor visa, then invitation, then latest

Among valid candidates for the target visa’s person (passport holder), pick in this order:

1. **Predecessor / extension orientation:** resolve the **preceding visa** on the same passport (latest other visa with `IssueDate` strictly before the new visa’s `IssueDate`, or latest other visa when IssueDate is unset). Prefer an eligible `ApplicationItem` whose **`CurrentVisa`** is that predecessor (typical visa-extension / transfer lines that extend the prior visa).
2. Else prefer the **last** eligible application that already has an **unused** `InvitationItem` when the type has `CanIssueInvitation`.
3. Else fall back to the latest eligible application (even if it has no invitation yet).

**Sort key** within each preference band: `Application.Date` descending, then `Application.ID` descending.

- If that application’s type has `CanIssueInvitation`, also default `InvitationItem` to the matching unused invitation line for that person (Path A invitation matcher).
- If not, leave `InvitationItem` null.

> **Ops note:** Path A eligibility depends on `ApplicationType.CanIssueVisa` / `CanIssueInvitation` in the database. If those columns were added with DEFAULT false and seed sync did not re-run, every candidate is excluded (empty links). Fix via `ApplicationTypeConfigurationUpdater` / capability-flag schema updater (`FORCE_XAF_DB_UPDATE` when ModuleInfo is already current), or backfill from `ApplicationTypeConfigurationCatalog.json`.

### 3.6 UI: read-only, code-set

- `IssuingApplicationItem` and `InvitationItem` on `Visa` are **read-only** for the user.
- Values are set by Path A matcher code (defaults + any sync when passport / dates change).
- User may **see** the values for transparency; user must **not** guess or manually pick them (reduces mistakes).

### 3.7 Auto-default on create (Path A entry point)

**Trigger:** run Path A matcher **only once on new Visa create** (not again on later `Passport` / `IssueDate` edits before/after first save).

When enough context exists at create (at least `Passport` / person; `IssueDate` when available for chronology filtering):

1. Run Path A **ApplicationItem** matcher (`CanIssueVisa` or `CanIssueInvitation`) → set `IssuingApplicationItem`.  
2. If that application type has `CanIssueInvitation`, run Path A **InvitationItem** matcher → set `InvitationItem`.  
3. Persist / validate so single-use, type rules, and chronology cannot be violated.

**No match:** leave `IssuingApplicationItem` / `InvitationItem` null and **allow save** (do not block the officer).

### 3.8 Side effect: mark invitation item used

When Path A sets `Visa.InvitationItem`, set **`InvitationItem.IsUsed = true`** automatically (respect exclusive flags: Cancelled / Changed / Used — linking as used clears/forbids the others per existing `InvitationItem` rules).

---

## 4. Path B — Import Visa (VISA2014 → Visa2026)

**Scope:** Only when importing `Visa` from legacy data.  
**Implementation home:** `Visa2026.DataImporter` / `.cursor/skills/visa2014-to-visa2026-import`.  
**Must not** call Path A (§3) auto-default or UI validation as the import matcher.

### 4.0 Path B matching responsibilities

Path B owns a **separate** matcher that, for each imported `Visa`:

1. **Match `ApplicationItem`** among already-imported eligible types (`CanIssueVisa` ∨ `CanIssueInvitation`) → set `IssuingApplicationItem`.
2. **If** that application’s type has `CanIssueInvitation` → **separately match `InvitationItem`** among already-imported invitation lines → set `InvitationItem`.
3. If not invitation-issuing → leave `InvitationItem` null.

**Hybrid algorithm (decided):** `IssuingApplicationItem` stays on the existing legacy ProcessNumber / extension-sibling post-pass; `InvitationItem` uses a **separate** target-side closest-match after that pass. Shared eligibility (§2) still applies for when invitation linking is attempted.

### 4.1 Matching process (hybrid — complete)

#### 4.1.1 Goal

For each imported `Visa`, find and set:

- `Visa.IssuingApplicationItem` → via **legacy ProcessNumber / extension-sibling** correction (not target closest-match)
- `Visa.InvitationItem` → closest matching target `InvitationItem` **only when** the issuing application type has `CanIssueInvitation`; otherwise leave null

#### 4.1.2 IssuingApplicationItem (existing post-pass + predecessor)

CLI: `--correct-visa2014-issuing-application-item`  
Code: `Visa2014VisaIssuingApplicationItemIndex` + `Visa2014VisaIssuingApplicationItemCorrection`

**Predecessor concept:** when a new visa is issued from a **visa extension** (or similar) flow, the issuing application line references the **preceding visa** on the same passport (`PersonInApplication.Visa` / target `ApplicationItem.CurrentVisa`). Matching may therefore orient on that predecessor, not only on ProcessNumber.

Priority:

1. Legacy `Visa.ProcessNumber` → `PersonInApplication` when that application is extension subtype **7**
2. Else **extension sibling / predecessor:** previous passport visa (issued earlier) that sits on a subtype-7 PIA → that PIA is the issuing line for the *next* visa
3. Else other ProcessNumber (e.g. invitation) when still unset
4. Else **target-side predecessor fallback:** if still unset after id-map apply, on Visa2026 find predecessor visa on same passport and an eligible `ApplicationItem` with `CurrentVisa` = that predecessor (not already linked to another visa)

Greenfield Visa POST leaves `IssuingApplicationItem` null; this correction runs after ApplicationItem id-map exists. Steps 1–3 are legacy-SQL oriented; step 4 is target-only and still must **not** call Path A Module helpers.

#### 4.1.3 InvitationItem closest-match (Path B only)

CLI: `--correct-visa2014-invitation-item` (runs **after** issuing-application-item correction)  
Code: `Visa2014VisaInvitationItemLinkMatcher` + `Visa2014VisaInvitationItemCorrection`  
**Must not** call `VisaIssuingLinkPathAMatcher`.

| Step | Rule |
|------|------|
| Gate | Skip unless `Visa.IssuingApplicationItem` is set and issuing app type has `CanIssueInvitation` |
| Person | Invitation item person = visa passport holder |
| Application | `Invitation.Application` = issuing application |
| Exclude | `IsCancelled` / `IsChanged` / `IsUsed`; invitation item already linked to another visa |
| Soft chronology | Prefer candidates with `IssuedDate > Application.ApplicationDate`; if none, fall back to remaining candidates (dirty legacy) |
| Closest match | When `Visa.IssueDate` set: require `IssuedDate < IssueDate`, pick **smallest gap** `IssueDate − IssuedDate`, tie-break `Invitation.ID` DESC. When IssueDate unset: latest `IssuedDate`, then `Invitation.ID` DESC |
| No match | Leave null; count/log skip; **do not fail** |
| Side effect | Set `InvitationItem.IsUsed = true` when linking |
| Idempotent | Already-correct link counts as already correct |

#### 4.1.4 Checklist

- [x] Person / passport identity match rules  
- [x] Date proximity / ordering rules  
- [x] Handling of cancelled / changed / already-used invitation items  
- [x] Ambiguity / no-match / multi-match behavior (leave null; closest gap; tie-break)  
- [x] Interaction with existing Visa field maps / id-maps (POST null; two post-passes)  
- [x] Dirty legacy: soft chronology fallback; still enforce single-use via exclude already-linked  
- [x] Explicit isolation from Path A Module helpers  

---

## 5. Open questions / clarifications

| # | Topic | Status |
|---|--------|--------|
| Q1 | Exact “InvitationItem Issued Date” field for chronology (Path A) | **Resolved:** use `Invitation.IssuedDate` (header formalization date) |
| Q2 | Strict `>` vs `≥` for chronology (Path A) | **Resolved:** strict `>` — `Visa.IssueDate` > `Invitation.IssuedDate` > `Application.Date` |
| Q3 | “Last” application sort key (Path A) | **Resolved:** `Application.Date` DESC, then `Application.ID` DESC |
| Q4 | Cancelled / ineligible candidates | **Resolved:** exclude cancelled app/item/progress; exclude invitation `IsCancelled` / `IsChanged` / already `IsUsed` |
| Q5 | Does linking set `InvitationItem.IsUsed` automatically in UI? | **Resolved:** yes on Path A link |
| Q6 | Both links together? | **Resolved:** match `ApplicationItem` when `CanIssueVisa` ∨ `CanIssueInvitation`; match `InvitationItem` only when `CanIssueInvitation` |
| Q7 | Eligibility for IssuingApplicationItem | **Resolved:** `CanIssueVisa` **or** `CanIssueInvitation` (union); InvitationItem when `CanIssueInvitation` |
| Q8 | Path B import “closest match” algorithm | **Resolved (hybrid):** ProcessNumber/sibling for IssuingApplicationItem; target closest-match for InvitationItem (§4.1) |
| Q9 | Two matchers | **Resolved:** Path A (manual / Module) and Path B (import / DataImporter) are separate; do not share one implementation |
| Q10 | No match (Path A) | **Resolved:** leave null; allow save (optional; do not frustrate officers) |
| Q11 | Re-match triggers (Path A) | **Resolved:** only on new Visa create (not on later Passport/IssueDate edits) |
| Q12 | Implementation order | **Resolved:** Path A first; Path B after §4.1 is complete |
| Q13 | InvitationItem under issuing app | **Resolved:** same `Invitation.Application` as issuing app; then latest `Invitation.IssuedDate` |
| Q14 | Read-only UI visibility | **Resolved:** always visible; `AllowEdit=False`; not gear-hidden |
| Q15 | Path A code | **Resolved:** implemented `VisaIssuingLinkPathAMatcher` + Visa wiring |

---

## 6. Out of scope (for now)

- Replacing ProcessNumber / extension-sibling with full target closest-match for `IssuingApplicationItem`.
- Changes to invitation **cancel** / **change** application flows except as they affect candidate eligibility.
- Binary / document import for visas or invitations.

---

## 7. Completion checklist

When the author finishes describing the process, mark:

- [x] §3 Path A (manual) matching complete and unambiguous — ApplicationItem + InvitationItem  
- [x] §4 Path B (import) matching complete and unambiguous — hybrid IssuingApplicationItem + InvitationItem  
- [x] §5 Path A and Path B open questions resolved  
- [x] **Ready for Path A implementation**  
- [x] **Ready for Path B hybrid implementation**

**Document complete:** Path A + Path B hybrid (§4.1).