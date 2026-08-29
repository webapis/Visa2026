# Create template from scan — user prompts

Copy-paste in Cursor chat (prefix `@visa2026-template-scan` if supported).

## Screenshot pack (preferred)

Paste screenshots and say e.g.:

- Here is Create from scan step-by-step (Upload → Done). Original scan is the last image. Improve from this.
- Review + Preview + catalog merge Preview vs original — layout still wrong.
- Full pack: Upload, Review, Generate, Preview, Done, catalog Preview, original scan.

Agent should compare, fix if needed, and **append learnings.md**.

## Bugs

- Create from scan Analyze failed on this PNG — show the real error and fix Azure deployment / key.
- Yellow highlights mapped wrong / missing ADAT next to application number.
- Draft is a flat Label: token list instead of the Turkmen letter.
- Header puts the date on the right; addressee should be opposite №/date.
- Duplicate gap for a compound yellow string that was already split into tokens.
- Urgency `Adaty tertipde!` should use `Urgency_NameTm`.

## Product / UX

- Wizard Preview must stay outline-only — do not embed the template preview-slot PDF viewer.
- Keep letter alignment (two-column header/signature, justify body, italic urgency).
- Only yellow-highlighted values become placeholders.

## Excel from scan

- Generate an Excel merge template from this scanned table/list document.
- Excel-from-scan should keep grid structure and row placeholders, not a flat token list.
- Yellow cells on this spreadsheet photo should map to Excel library tokens.

## Config

- Point TemplateAiScan Azure deployment at the same vision model Convert uses.
- Profile is locked — confirm we can still add a **new** template from scan.

## Triage

- Is this Create from scan, Convert existing document, Resminamalar merge, or preview-slot CSS?