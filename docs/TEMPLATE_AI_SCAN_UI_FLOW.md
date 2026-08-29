# Template AI scan — UI interaction scenario

**Status:** Draft — prototypes **01–12** under [`docs/prototypes/`](prototypes/)  
**Canonical UX:** [`TEMPLATE_AI_SCAN_PRODUCT_SPEC.md`](TEMPLATE_AI_SCAN_PRODUCT_SPEC.md) (decisions S1–S13)  
**Sibling feature:** [`TEMPLATE_AI_CONVERT_UI_FLOW.md`](TEMPLATE_AI_CONVERT_UI_FLOW.md) (Convert — do not merge modals)  
**Engineering:** [`TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md`](TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md)

This document defines **which control the officer touches and what happens next** for **Create from scan**. Build Blazor against this table, not the PNGs alone.

---

## 1. Views

| ID | View | PNG | Where it lives |
|----|------|-----|----------------|
| **V0** | Entry points (wizard · case workspace) | (host background) | Host page, not modal |
| **V1** | Upload scan | `01-upload` | Modal, narrow |
| **V2** | Field review (vision proposals) | `02-field-review` | Modal, wide |
| **V3** | Clarification chat | `03-clarification-chat` | Modal, wide (overlay or tab on V2) |
| **V4** | Generating draft | `04-generating` | Modal, narrow |
| **V5** | Preview + validate + Approve | `05-preview-approve` | Modal, wide |
| **V6** | Done | `06-done` | Modal, wide |
| **V7** | Scan quality **Fail** | `07-scan-fail` | V1/V2 variant |
| **V8** | Low confidence **Warn** | `08-low-confidence-warn` | V2 variant |
| **V9** | Needs help — gaps | `09-needs-help-gaps` | Modal, wide |
| **V10** | Multi-page PDF | `10-multipage-pdf` | V1 variant |
| **V11** | Optional instance context | `11-instance-context` | V1 variant |
| **V12** | AI off | `12-ai-off` | Entry + disabled modal hint |

---

## 2. Global rules

| Control / event | Behavior | Guard |
|-----------------|----------|-------|
| **X** · **Cancel** · backdrop · **Esc** | Close modal → V0 | After V1 analyze: **discard confirm** |
| Modal opens | **V1**, state cleared | — |
| Stepper | Display only | Upload → Review → Generate → Preview → Done |
| Config lock | Banner: “Profile locked — new templates only” | Approve **enabled** for new template; edit existing blocked |
| AI off | Entry visible, disabled + badge | Add prepared template + Convert unchanged |

---

## 3. V0 — Entry points

| Element | Where | Action | Result |
|---------|-------|--------|--------|
| **Create from scan** | Wizard Templates step | Open modal V1 `source=wizard` | Optional instance picker (V11) |
| **Create from scan** | Case Resminamalar row | Open modal V1 `source=instance` | Instance read-only context if scan filled |
| **Convert existing document** | Same rows | Opens **Convert** modal | Unchanged |
| **Add prepared template** | Same rows | Manual upload modal | Unchanged |

---

## 4. V1 — Upload scan

| Element | Type | Action | Result |
|---------|------|--------|--------|
| Template name | text | Edit | Required for Analyze |
| Catalog target | radio | Profile-specific / Shared | Shared confirmed at Approve |
| Data scope | select | Header / People / Both | Filters placeholder set |
| Scan type | radio | Blank form / Filled sample | Filled → show instance picker (V11) |
| File | drop zone | PNG, JPG, PDF | Multi-page PDF → V10 thumbnails |
| **Analyze scan** | primary | Click | → suitability → V2 or V7 |
| Add prepared template | link | Click | Leave for L12 manual path |

**Analyze guard:** file + name + (filled ⇒ instance selected).

---

## 5. V2 — Field review

| Element | Type | Action | Result |
|---------|------|--------|--------|
| Scan viewport | display | Hover box | Shows label + proposed token |
| Field list | table | — | Confidence chips; orange gaps |
| **Ask for clarification** | secondary | Click | → V3 chat |
| **Continue** | primary | Click | → V4 generating |
| Upload different scan | link | Click | → V1 |

**Continue guard:** suitability pass; if V8 warnings, checkbox acknowledged.

---

## 6. V3 — Clarification chat

| Element | Type | Action | Result |
|---------|------|--------|--------|
| Chat thread | display | — | AI questions; officer answers |
| Quick replies | chips | Click | Pre-filled answers |
| Message input | text | Send | Updates field plan |
| **Back to field list** | link | Click | → V2 with revised plan |
| Reject banner | display | — | Shown if officer asks out-of-scope (S8) |

---

## 7. V4 — Generating

| Element | Type | Action | Result |
|---------|------|--------|--------|
| Progress | display | — | “Building draft Word template…” |
| — | — | auto | → V5 on success; error rail on failure |

---

## 8. V5 — Preview + Approve

| Element | Type | Action | Result |
|---------|------|--------|--------|
| Draft preview | iframe/PDF | — | Generated docx preview |
| Placeholders tab | list | — | Extract output |
| Validate rail | display | errors/warnings | Hard fail blocks Approve |
| Gap summary | display | — | Count of orange fields |
| **Approve — save to profile** | primary | Click | → V6 |
| **Needs help** | secondary | Click | → V9 |
| Regenerate | link | Click | → V4 |

---

## 9. V6 — Done

| Element | Type | Action | Result |
|---------|------|--------|--------|
| Success message | display | — | Template name + catalog scope |
| Open in wizard | link | wizard only | Focus new row |
| Close | primary | Click | Refresh catalog / close modal |

Wizard path: officer still clicks **Save profile** to persist (same as Convert wizard entry).

---

## 10. Edge views (summary)

| ID | Trigger | Officer exit |
|----|---------|--------------|
| V7 | OCR fail / corrupt PDF | Upload again · Cancel |
| V8 | Low confidence fields | Acknowledge · fix via chat · Cancel |
| V9 | Gaps | Export packet · Approve anyway · Cancel |
| V10 | PDF &gt; 1 page | Select pages · Analyze |
| V11 | Filled scan | Pick case for value hints |
| V12 | Provider None | Use Add prepared template |

---

## Revision log

| Date | Change |
|------|--------|
| 2026-08-28 | Initial views V0–V12 aligned to prototypes 01–12 |
