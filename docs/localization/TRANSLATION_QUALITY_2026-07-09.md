# Translation quality scan — 2026-07-09

Automated orthography/lexicon scan of **~1,400** leaf strings (UiStrings + Layer B lookup JSON). Country names excluded from “same as English” checks.

**Important:** This is a **heuristic** QA pass (Turkish letters in `tk-TM`, English leftovers, copy-from-tr). It cannot certify meaning for every string; a native Turkmen reviewer should still spot-check officer-facing UI.

## Verdict

| Area | Result |
|------|--------|
| Turkmen orthography (`ı` / `ğ` in `tk-TM`) | **2 real bugs** (fixed in this pass) |
| Untranslated English sentences in `tk-TM` | **None** in real UI copy (only sample passport numbers, intentional) |
| Turkish lexicon left in `tk-TM` (`olarak`, `musunuz`, …) | **None found** |
| Layer B ministry 3–5 / migration-service (recent) | Pattern-consistent with existing 1st/2nd ministry strings |
| Recent hard-coded → messages (Person/Passport/Legacy sync/Runtime log) | Present in all 4 languages; no English left in `tk-TM` |

## Fixed (Turkmen orthography)

Turkish **dotless ı** is not used in Turkmen Latin; use **y**.

| Key | Was (tk-TM) | Fixed (tk-TM) |
|-----|-------------|---------------|
| `application-type.App_Reg_Info_Change_Passport` | … (Pasport **Çalışmagy**) | … (Pasport **Çalyşmagy**) |
| `ApplicationLifecycleStage.Stay` | **Galış** | **Galyş** |

After enum fix, re-run: `dotnet run --project tools/GenerateModelLocalization/GenerateModelLocalization.csproj`

## False positives / ignore

| Finding | Why ignore |
|---------|------------|
| Sample `DisplayKey` values (`TM 12 3456789`, …) | Demo IDs, same in all cultures by design |
| Country names same in en/tk | Expected for many ISO names |
| `Resminamalar` same in all languages | Product term |
| Capital **İ** in Turkmen | Valid in Turkmen Latin (not the same as Turkish **ı**) |

## Suggested native review (meaning, not auto-detectable)

These are **not** flagged as wrong by heuristics; worth a Turkmen speaker glance:

| Key | en | tk-TM (current) | Note |
|-----|----|-----------------|------|
| `UserFeedback.Action.MarkInProgress` | Mark in progress | Işlenilýär diýlip bellen | Shorter than RuntimeLog’s `… belläň`; align style? |
| `LegacySync.OpenInNewTab` | Open in new tab | Täze goýmada aç | Confirm “goýma” = browser tab in your glossary |
| `StateChangeLog.*.ToolTip` | Navigate to the object… | … obýekte geçiň | Loanword `obýekt` OK for admin UI? |
| `RuntimeLog.Confirm.*` | Mark selected runtime error(s)… | … iş wagty ýalňyşlyklaryny … | Long; confirm “runtime error” wording for officers/devs |

## Artifacts

- `docs/localization/translation-quality-refined.csv`
- `docs/localization/translation-quality-high.json`
- Earlier broad scan CSVs under `docs/localization/` (many false positives from treating **İ** as Turkish-only)

## Re-scan command idea

Flag only Turkish-specific letters in `tk-TM`: **ı** (U+0131), **ğ/Ğ** — not **İ**.