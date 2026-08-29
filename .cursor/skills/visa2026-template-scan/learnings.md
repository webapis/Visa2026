# Create template from scan — learnings

Append-only. Newest first under **## Entries**.

## Entries

### 2026-08-29 — Ghost teal boxes between paragraphs (v3)

- Symptom: Review shows correct tokens; urgency/header often OK; body has teal boxes floating in whitespace above yellow ink (`6 (alty) aý` missed); extra ghost boxes between paragraphs.
- Cause: Sparse warm pixels / dilated samples created low-density “yellow” blobs in the gap. AssignBoxes then matched upward-shifted AI boxes to those ghosts (closer in Y than real ink).
- Fix: Density filter on detected blobs (≥12% yellow pixels). Score matches by horizontal overlap + prefer yellows at/below AI Y (not nearest Y). Pale highlighter RGB/HSV accepted.
- Verify: Ctrl+F5 → re-Analyze → body teal boxes sit on `18 (on sekiz)`, `6 (alty) aý`, `köp gezeklik`; no empty boxes in the paragraph gap.
- Prevent: Never zip fields to yellows by Y-order alone when AI boxes are vertically drifted.
- Cross-skill: -
### 2026-08-29 — Review boxes still above body yellow (v2)

- Symptom: After yellow snap, header/urgency OK; body teal boxes floated in white space above paragraph yellow ink; some ghost empty boxes.
- Cause: (1) Stage `aspect-ratio` from WidthPx/HeightPx could disagree with displayed PNG → vertical drift lower on page. (2) AssignBoxes trusted AI IoU order. (3) MergeNearby glued vertically separated blobs into tall regions.
- Fix: Overlays sit in `tas-scan-overlays` sized to the image (`inset:0` on stage wrapping img only; no aspect-ratio). Assign fields to yellow blobs by document token order only. Merge yellows only on the same line.
- Verify: Ctrl+F5 → Analyze → Review body boxes on `18` / `on sekiz` / `6 (alty) aý` / `köp gezeklik` yellow ink.
- Prevent: Do not position overlays on a stage whose aspect ratio is independent of the `<img>` box.
- Cross-skill: -
### 2026-08-29 — Review teal boxes misplaced vs yellow ink

- Symptom: Upload/Analyze OK; Review mapped 7 tokens correctly but teal candidate squares sat on empty space / non-yellow text (e.g. company name), not on yellow highlights.
- Cause: Vision returns coarse/wrong normalized boxes; local yellow splits reused the parent box. Overlay CSS % positioning was fine.
- Fix: `ScanYellowRegionDetector` finds highlighter blobs on the page PNG; `ScanFieldBoxLocalizer` snaps field boxes to those regions after merge. Resolver slices compound parent boxes by snippet index. Stronger AI box prompt.
- Verify: Unit `ScanFieldBoxLocalizerTests`. Officer: hard-refresh → Analyze same yellow letter → Review overlays sit on yellow ink; hover row ↔ box.
- Prevent: Do not trust vision boxes alone for Review overlays when yellow ink is detectable on the PNG.
- Cross-skill: -
### 2026-08-29 — Screenshot-per-step feedback is the experience engine

- Need: Officer will submit a screenshot for each Create-from-scan step so the skill accumulates experience and improves generation.
- Fix: SKILL **Screenshot feedback loop** + MATURITY screenshot-driven loop + prompts pack openers. Agent must append learnings after every pack (good or bad).
- Verify: Next chat with Upload…Done images → compare → fix or confirm → learnings entry with Steps attached.
- Prevent: Do not treat screenshot packs as one-off UI comments without logging experience.
- Cross-skill: -
### 2026-08-29 — Skill mission: Word and Excel from scans

- Need: Skill should drive improving generation of **Word and Excel** templates from scanned document images, not Word-only forever.
- Fix: SKILL mission + scope include Excel-from-scan as improvement target; Word remains shipped path. Product S3 (Word-only v1) noted until Excel ships and specs are updated.
- Prevent: Do not treat Excel-from-scan as permanently out of scope or dump it into Convert.
- Cross-skill: visa2026-user-report-templates (Excel merge families)
### 2026-08-29 — Skill carved out from application-profile

- Need: Create from scan had grown enough (yellow gate, layout normalizer, Azure vision) to own a dedicated skill.
- Fix: `.cursor/skills/visa2026-template-scan/` (SKILL, reference, prompts, MATURITY, learnings). Cross-link from AGENTS + application-profile.
- Verify: Agent loads this skill for Create from scan / TemplateScan work.
- Prevent: Do not dump scan pipeline fixes only into application-profile learnings.
- Cross-skill: application-profile | resminamalar | preview-slot | user-report-templates

### 2026-08-29 — Wizard Preview is outline only (no preview-slot PDF)

- Need: Officers do not want the Resminamalar/template PDF preview viewer inside Create template from scan.
- Fix: `TemplateScanPreviewView` uses `TemplateConvertOutlineView`. Page layout after save via catalog Preview / Edit template.
- Prevent: Do not inject `ApplicationWordReportOfficePreviewPdfConverter` into the scan wizard.
- Cross-skill: visa2026-preview-slot | visa2026-resminamalar

### 2026-08-29 — Letter layout: AFNUM+ADAT left, addressee right

- Symptom: Draft/merge put date on the right; addressee missing or stacked left.
- Cause: Vision emitted `twoColumn` as AFNUM \| ADAT; normalizer did not recover addressee.
- Fix: `ScanLetterLayoutNormalizer` rebuilds header (left = number+date, right = addressee); OCR inject when AI drops recipient; prompt forbids ADAT-only right cell.
- Verify: Unit `ScanLetterLayoutNormalizerTests`; officer catalog Preview after Approve.
- Cross-skill: -

### 2026-08-29 — Yellow-highlight-only + compound split

- Rule: Map only yellow spans; Fail if no yellow / yellow unmapped; `ScanYellowHighlightTokenResolver` splits AFNUM/ADAT, TPCNT/TPCTX, VPER/VCAT, Urgency_NameTm; drop duplicate compound gaps.
- Prevent: Do not fall back to OCR inventing non-yellow fields when vision fails.
- Cross-skill: visa2026-user-report-templates (catalog)

### 2026-08-29 — Analyze DeploymentNotFound on gpt-4o-mini

- Cause: Scan deployment gone on Azure resource; Convert already on `gpt-4.1-mini`.
- Fix: Point `TemplateAiScan:AzureOpenAI:Deployment` at working vision deployment; surface `ex.Message`.
- Prevent: Probe deployment before renaming; prefer Convert’s deployment on same resource.
- Cross-skill: -

### 2026-08-29 — No leftover-token dump at footer

- Cause: ParseLayout appended every unused mapped token → ruined letter.
- Fix: Remove footer dumps; place tokens in-context; warn if Review token not in draft.
- Prevent: Never append unused merge tokens as extra paragraphs.
- Cross-skill: -

### 2026-08-29 — Placeholder Manual missing tokens (Application alias)

- Cause: Catalog JSON `rootBoTypes: ["Application"]` ≠ enum `ApplicationProfileInstance`.
- Fix: Alias in `UserReportPlaceholderCatalogService.ParseRootBoTypes`.
- Cross-skill: visa2026-user-report-templates