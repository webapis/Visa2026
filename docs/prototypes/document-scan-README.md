# Document scan field extraction — UI prototypes

Officer uploads a copy of a real document; the system proposes field values; the officer reviews and accepts; the BO is saved **together with** the file.

Canonical: [`DOCUMENT_SCAN_FIELD_EXTRACTION.md`](../DOCUMENT_SCAN_FIELD_EXTRACTION.md) · Status: **Phase 0 plan, nothing implemented**. These are **illustrations of the locked decisions**, not a spec of final pixels.

| # | File | Screen | Decision shown |
|---|------|--------|----------------|
| 01 | [`document-scan-01-upload.png`](./document-scan-01-upload.png) | Upload a copy — accepted formats, size cap, several files per document | X-D1, X-D11, X-D13 |
| 02 | [`document-scan-02-extracting.png`](./document-scan-02-extracting.png) | Extraction pipeline: pages → quality → MRZ → OCR → AI (skipped) | **X-D2** deterministic before probabilistic |
| 03 | [`document-scan-03-passport-review.png`](./document-scan-03-passport-review.png) | **Core screen.** Passport review: per-field source, confidence, accept; blanks for `IssueDate` / `Authority`; person verification band | X-D5, X-D6, X-D7, X-D10 |
| 04 | [`document-scan-04-page-set.png`](./document-scan-04-page-set.png) | Work permit as one page set over 3 files; page relevance scores; handwritten number/date skipped | **X-D13**, §0a handwriting |
| 05 | [`document-scan-05-unrecognised-layout.png`](./document-scan-05-unrecognised-layout.png) | New form version: anchors missing, profile scores below threshold, degrade + report gap packet | **X-D12** layout drift |
| 06 | [`document-scan-06-visa-verification.png`](./document-scan-06-visa-verification.png) | Issued visa compared against the case; one mismatch flagged, nothing overwritten | **X-D5** verification mode |
| 07 | [`document-scan-07-quality-gate.png`](./document-scan-07-quality-gate.png) | Phone photo rejected: below `MinShortSidePixels`, MRZ unreadable, stamp + skew | **X-D6** blank beats wrong |
| 08 | [`document-scan-08-duplicate-guard.png`](./document-scan-08-duplicate-guard.png) | Read number matches an active passport → open existing instead | **X-D8** |
| 09 | [`document-scan-09-invitation-fill-gap.png`](./document-scan-09-invitation-fill-gap.png) | Invitation matches; one **empty** case field offered for fill (`AS-621785` → `AS621785`) | X-D5 (empty-only), §0a gap |

## UX conventions across the set

1. **Two panes always:** the copy on the left with a zoom toolbar, the field list on the right. Host is `#visa-preview-slot`, not an XAF modal — see [`visa2026-preview-slot`](../../.cursor/skills/visa2026-preview-slot/SKILL.md).
2. **Every proposed value shows its source** as a chip (`MRZ ✓` green / `OCR` amber / `AI` / `PDF text`) plus a confidence dot. Provenance is visible, not hidden in audit (X-D10).
3. **Accept is per field** with a checkbox. Nothing below `MinAcceptConfidence` is pre-checked; it renders as an amber `Needs entry` input (X-D6, X-D7).
4. **Verification is visually distinct from prefill.** Case-side records (`Invitation`, `WorkPermit`, issued `Visa`) use a *comparison table* — "On the copy" beside "In this case" — with `Match` / `Differs` / `Can fill` results. The primary button never says "apply" there (X-D5).
5. **Refusal is a designed screen, not an error toast** (07 and 05). The officer always gets a way forward: manual entry, better copy, or report the form version.
6. **Green means verified, amber means read but unsure, red means differs or rejected** — consistent with [`BO_STATE_COLORS.md`](../BO_STATE_COLORS.md) tones.

## Open UX questions

- Where the passport entry point lives on the `Person` DetailView (toolbar action beside **New passport**, or inside the passport DetailView).
- Whether verification (06 / 09) runs automatically on attach or only on an explicit **Check copy** action.
- Whether **Fill empty fields** (09) needs its own permission, separate from creating records from a scan.
