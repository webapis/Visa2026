# Product spec — AI create profile template from scan/image

> **Status:** Draft product spec (not implemented)  
> **Audience:** Template authors / officers with template-authoring permission  
> **Separate from:** [`TEMPLATE_AI_CONVERT_PRODUCT_SPEC.md`](TEMPLATE_AI_CONVERT_PRODUCT_SPEC.md) — **Convert existing document** reverse-maps a **filled Word/Excel**; this feature **creates** a draft template from a **scan or photo**.  
> **Related:** [`USER_TEMPLATE_AUTHOR_GUIDE.md`](USER_TEMPLATE_AUTHOR_GUIDE.md), [`USER_REPORT_MAP_STANDARD.md`](USER_REPORT_MAP_STANDARD.md), [`TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md`](TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md) (reuse save/validate/placeholder set), [`APPLICATION_PROFILE_PLAN.md`](APPLICATION_PROFILE_PLAN.md)  
> **UI scenario:** [`TEMPLATE_AI_SCAN_UI_FLOW.md`](TEMPLATE_AI_SCAN_UI_FLOW.md)  
> **Engineering:** [`TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md`](TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md) — Phase 0 contracts, **SD-D1–D10**, slices **S1–S7**  
> **Non-goal:** Pixel-perfect recreation of ministry letterhead, stamps, or complex graphics from a photo.  
> **Non-goal:** Replacing developer `_map.md` approval for production-grade ministry baselines (optional export only in v1).  
> **Locked:** Separate entry point and modal — never merged into Convert.  
> **Locked:** AI may **only** emit placeholders from the **target Application Profile** vocabulary (same L10 rule as Convert).  
> **Locked:** Officer **must** preview and Approve; no silent catalog publish.

---

## 0. UI prototypes (draft)

Generated UX sketches (not production screens). Saved under [`docs/prototypes/`](prototypes/).

### Happy path

| # | Scenario | File |
|---|----------|------|
| 01 | Upload scan + metadata | [`template-ai-scan-01-upload.png`](prototypes/template-ai-scan-01-upload.png) |
| 02 | Layout analysis — detected fields + placeholder proposals | [`template-ai-scan-02-field-review.png`](prototypes/template-ai-scan-02-field-review.png) |
| 03 | Clarification chat (AI asks, officer answers) | [`template-ai-scan-03-clarification-chat.png`](prototypes/template-ai-scan-03-clarification-chat.png) |
| 04 | Generating draft Word template | [`template-ai-scan-04-generating.png`](prototypes/template-ai-scan-04-generating.png) |
| 05 | Preview + validate + Approve | [`template-ai-scan-05-preview-approve.png`](prototypes/template-ai-scan-05-preview-approve.png) |
| 06 | Done (saved to profile catalog) | [`template-ai-scan-06-done.png`](prototypes/template-ai-scan-06-done.png) |

### Alternate / edge scenarios

| # | Scenario | File |
|---|----------|------|
| 07 | Scan quality **Fail** (unreadable / too small) | [`template-ai-scan-07-scan-fail.png`](prototypes/template-ai-scan-07-scan-fail.png) |
| 08 | Low-confidence fields — acknowledge checkbox | [`template-ai-scan-08-low-confidence-warn.png`](prototypes/template-ai-scan-08-low-confidence-warn.png) |
| 09 | **Needs help** — gaps (no library token) | [`template-ai-scan-09-needs-help-gaps.png`](prototypes/template-ai-scan-09-needs-help-gaps.png) |
| 10 | Multi-page PDF upload | [`template-ai-scan-10-multipage-pdf.png`](prototypes/template-ai-scan-10-multipage-pdf.png) |
| 11 | Optional **filled scan + case context** (mapping boost) | [`template-ai-scan-11-instance-context.png`](prototypes/template-ai-scan-11-instance-context.png) |
| 12 | AI off — entry disabled, manual paths remain | [`template-ai-scan-12-ai-off.png`](prototypes/template-ai-scan-12-ai-off.png) |

Full set: **01–12** (happy path + edges).

---

## 1. Goal

Officer uploads a **yellow-marked Word or Excel** file. Yellow marks are read from OpenXML (no vision). The system proposes **library placeholders**, optionally asks **clarification**, **writes tokens into a copy** of the upload (layout preserved), runs **Extract/Validate**, and lets the officer **Approve** after preview. **PNG/JPG/PDF are not accepted.**

Save target is always the **parent Application Profile** template catalog — same as Convert and manual Add.

| In scope (v1) | Out of scope (v1) |
|---------------|-------------------|
| Separate **Create from yellow marks** entry (wizard + optional case workspace) | Merging this flow into Convert |
| Yellow-marked Word (`.docx`) / Excel (`.xlsx`) only | PNG / JPG / PDF (retired); video; handwritten-only |
| Vision layout + label detection | Pixel-perfect scan reproduction |
| Proposed `{{ds.…}}` from **profile-scoped** placeholder set | Full system placeholder library |
| Clarification Q&A before generate | Open-ended “redesign this letter” chat |
| Draft `.docx` generation + preview | In-browser WYSIWYG editor |
| Ephemeral Extract/Validate before Approve | Auto-publish without officer review |
| Gap list + **Needs help** export stub | Guaranteed production `_map.md` approval in-app |
| Embedded **authoring playbook** (`.md` rules in provider prompt) | Cursor IDE as runtime provider |
| Reuse save bridge to `ApplicationProfileTemplate` | Excel roster-from-scan (defer to v2 unless paired with sample) |
| New templates while profile config locked | Editing existing template rows while locked |

---

## 2. Locked decisions — Scan vs Convert

| # | Topic | Decision |
|---|--------|----------|
| **S1** | **Feature identity** | **Create from yellow marks** (formerly Create from scan) is a **separate** product, modal, and orchestrator entry — not a mode inside Convert. |
| **S2** | **Input** | Image (PNG/JPG) or PDF. Max size follows staging limits (50 MB cap; warn above 20 MB). |
| **S3** | **Output format** | Tokenized **copy of uploaded** `.docx` / `.xlsx` (layout preserved). Image/PDF path **retired**. |
| **S4** | **Save target** | Parent **Application Profile** catalog (`ApplicationProfileTemplate` + bridged `UserReportTemplate`). Profile-specific default; Shared opt-in with confirm (same B/C as Convert). |
| **S5** | **Instance context** | **Optional.** Blank forms: no instance required. Filled scans: officer **may** pick **this case** to boost value→token matching (similar data to Convert L6, never mandatory). |
| **S6** | **Placeholder vocabulary** | Same as Convert **L10**: only tokens allowed for the **target profile** + chosen **data scope** + enabled person packs. |
| **S7** | **AI scope** | Vision may infer **field boundaries** and **semantic labels**. Generation may create **structure** (tables, rows) needed to hold placeholders. AI must **not** invent merge fields outside the allowed set; gaps → **Needs help**. |
| **S8** | **Clarification chat** | **Allowed** before generate: disambiguate labels (“application date vs contract date?”), roster vs header, optional vs required fields. **Not allowed:** restyle ministry boilerplate, translate prose for aesthetic reasons, remove legal paragraphs. |
| **S9** | **Authoring playbook** | Server ships **`Visa2026.Module/Resources/TemplateAuthoring/SCAN_AUTHORING_PLAYBOOK.md`** (excerpt of [`USER_TEMPLATE_AUTHOR_GUIDE.md`](USER_TEMPLATE_AUTHOR_GUIDE.md) + [`USER_REPORT_MAP_STANDARD.md`](USER_REPORT_MAP_STANDARD.md)). Every vision/generate provider call includes playbook hash in audit metadata. |
| **S10** | **Preview gate** | **Approve** disabled until ephemeral **Validate** passes or officer acknowledges **warnings** (same E-D2 pattern as Convert). Hard Validate errors block Approve. |
| **S11** | **Quality gate** | **Scan suitability** runs before field review: resolution, skew, OCR confidence, detectable text regions. Hard fail → retry upload; warn → acknowledge checkbox. |
| **S12** | **Config lock** | **New** templates may be added while locked (same carve-out as Convert post-fix). **Existing** template rows stay read-only. |
| **S13** | **Provider** | Reuse pluggable provider pattern (`ITemplateScanAiProvider` or extend Convert provider with scan methods). Config section: `TemplateAiScan:` sibling to `TemplateAiConvert:`. **None** disables entry; **Add prepared template** and **Convert** unaffected. |

### S5 — Optional instance payload (when enabled)

| Allowed | Forbidden |
|---------|-----------|
| Selected instance scalars + linked people for **value hints only** | Other instances, bulk DB reads |
| Redacted snapshot in vision follow-up (labels + matched values) | Sending unrelated Person rows |
| Target-profile placeholder **names/paths only** | Full catalog dump |

---

## 3. Entry points

| Location | Control | Visibility |
|----------|---------|------------|
| Profile wizard — Templates step | **Create from yellow marks** (beside Convert / Add template) | Template-authoring permission + `TemplateAiScan:Enabled` |
| Case workspace — Resminamalar action row | **Create from yellow marks** | Same permission + optional per-user **scan editor** switch (mirror L13 pattern; default **off** on case) |
| — | **Convert existing document** | Unchanged — filled Word/Excel only |
| — | **Add prepared template** | Always available (L12) |

---

## 4. Officer journey (summary)

```
Upload scan → Scan suitability → Field review (vision proposals)
    → Clarification chat (if needed) → Generate draft docx
    → Preview + Validate → Approve → Done (profile catalog)
```

**Discard:** Closing after upload analysis asks confirm (same discard pattern as Convert V7).

**Recovery:** Scan again · change data scope · answer chat · **Needs help** (gap packet) · Cancel.

---

## 5. Field review (core UX)

The officer sees a **split view**:

| Pane | Content |
|------|---------|
| **Scan viewport** | Uploaded image/PDF page with bounding boxes: green = mapped field, orange = gap, gray = static text |
| **Field list** | Rows: Label on scan · Proposed token · Confidence · Edit scope (header / row / loop) |

Actions: **Ask AI** (opens clarification chat) · **Continue to generate** (enabled when suitability pass + no blocking errors).

No spreadsheet-style mapping grid — list + highlights only.

---

## 6. Clarification chat (S8)

| Allowed questions / answers | Rejected |
|-----------------------------|----------|
| “Is field X the application number?” | “Make the letter shorter” |
| “Should this table repeat per person?” | “Switch to English” |
| “Use contract start instead of application date here” | “Change font to Arial” |

Chat turns update the **field plan** before generation; officer always sees the revised list.

---

## 7. Playbook (S9)

The playbook is **not** officer-editable at runtime. Versioned in repo:

| Section | Source |
|---------|--------|
| `ds.` prefix, loops, roster rules | `USER_TEMPLATE_AUTHOR_GUIDE.md` |
| Map sections §6–§7 token discipline | `USER_REPORT_MAP_STANDARD.md` |
| Profile placeholder set constraint | Convert E1 service rules |
| Gap behavior | This spec §9 |

Optional v2: export draft `_map.md` for developer review (not required for Approve in v1).

---

## 8. Gaps and Needs help

When a detected field has **no token** in the profile set:

| UI | Behavior |
|----|----------|
| Orange highlight + row in field list | “No placeholder — Needs help” |
| Approve | **Allowed** if officer acknowledges gaps (template saves; Resminamalar may show CHECK) |
| **Needs help** button | Exports gap packet: scan hash, field snippets, AI suggestions, profile id, optional instance id |

Same developer handoff intent as Convert prototype 12.

---

## 9. Permissions and config

| Gate | Rule |
|------|------|
| Permission | Write on `ApplicationProfileTemplate` + `UserReportTemplate` (same as Convert) |
| Feature flag | `TemplateAiScan:Enabled` (default false until slice ships) |
| Provider | `TemplateAiScan:Provider` = `None` \| `AzureOpenAI` \| … |
| Vision model | Configured per adapter (e.g. GPT-4o vision deployment) — not hard-coded in UI |

---

## 10. Phasing (implementation slices)

| Slice | Deliverable |
|-------|-------------|
| **S0** | Product + UI flow locked (this doc + PNGs) | **Done** 2026-08-28 |
| **S1** | `ITemplateScanOrchestrator` contracts, scan suitability, playbook loader | See [`TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md`](TEMPLATE_AI_SCAN_ENGINEERING_SPEC.md) §2 |
| **S2** | Vision adapter (Azure OpenAI vision / Document Intelligence spike) |
| **S3** | Field plan model + review UI (Blazor modal) |
| **S4** | Clarification chat + plan revision |
| **S5** | Draft docx generator + diff/validate gate |
| **S6** | Save + wizard/case entry points |
| **S7** | Needs help export, optional `_map.md` draft |

---

## 11. Success criteria (v1)

- Officer can upload a clear single-page ministry scan and receive a **draft Word template** with ≥80% of obvious data fields tokenized correctly on a golden set (TBD).
- All emitted tokens ∈ profile placeholder set (Validate enforced).
- Convert and Add prepared template unchanged.
- Separate modal, separate config, separate audit category (`TemplateScan`).

---

## 12. Open questions (resolve before S1)

| # | Question | Default proposal | Engineering lock |
|---|----------|------------------|------------------|
| O1 | Case workspace entry default | Off per user (mirror L13) | **SD-D10** / `ShowInstanceEntry = false` |
| O2 | PDF page limit v1 | 5 pages max | **SD-D3** |
| O3 | Persist in-progress scan draft (E4-like BO) | No — ephemeral bytes in modal until Approve | **SD-D5** |
| O4 | Turkmen OCR quality threshold | Fail &lt; 40% text confidence; warn 40–70% | **SD-D2** |

---

## Revision log

| Date | Change |
|------|--------|
| 2026-08-28 | Engineering spec + playbook stub; O1–O4 locked via SD-D2–D5/D10 |
