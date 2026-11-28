---
name: visa2026-template-edit
description: >-
  Edit template on the officer PC: local sandbox %LOCALAPPDATA%\Visa2026\TemplateEdit,
  visa2026edit opener, Word and Excel Home Sync add-in, Install on this PC zip, ribbon sync,
  and publishing the profile copy Preview uses. Use for Edit template, Sync to database,
  Home Sync missing, installer, sandbox folder, template not updating in Preview after Sync,
  or Office add-in load failures. Not Resminamalar catalog/ZIP (visa2026-resminamalar),
  not merge tokens (visa2026-user-report-templates), not yellow-marks (visa2026-template-scan).
  Read learnings.md first; append after verified fixes.
disable-model-invocation: false
---

# Visa2026 — Edit template (local sandbox and Office Sync)

**User prompts:** [prompts.md](./prompts.md) (`@visa2026-template-edit`).

## Agent workflow (every task — mandatory)

1. **Read** [learnings.md](./learnings.md) (**## Entries**, newest first) and **Scenarios** below.
2. **Classify** — sandbox, opener, add-in, sync, installer (**this skill**) vs catalog/ZIP/readiness (**[resminamalar](../visa2026-resminamalar/SKILL.md)**) vs `{{token}}` merge (**[user-report-templates](../visa2026-user-report-templates/SKILL.md)**).
3. **Fix** with minimal diff. Logic stays in **Visa2026.Module**. The Blazor host stays a thin shell. Sync must update the master **and** the same-name profile copy Preview uses.
4. **Verify** — isolated build of `Visa2026.Blazor.Server` when C# changes (do not write repository `bin/` while F5 is running). Add-in changes: reinstall and confirm Word and Excel `COMAddIns` `Connect` is true. Officer path: Edit template → save → Home Sync → Preview.
5. **Record** — append [learnings.md](./learnings.md) after a **verified** fix ([MATURITY.md](./MATURITY.md)). Update [`docs/TEMPLATE_STAGING_EDIT.md`](../../../docs/TEMPLATE_STAGING_EDIT.md) when the officer steps change.
6. **Promote** — same root cause twice → update **Scenarios** or [reference.md](./reference.md).

## Canonical doc

**[`docs/TEMPLATE_STAGING_EDIT.md`](../../../docs/TEMPLATE_STAGING_EDIT.md)** — officer steps, dev vs production, endpoints, file map.

**Related skills (do not duplicate):**

| Topic | Skill |
|-------|--------|
| Catalog, readiness, Preview shell content, ZIP | [visa2026-resminamalar](../visa2026-resminamalar/SKILL.md) |
| Placeholder merge, Extract/Validate, seeds | [visa2026-user-report-templates](../visa2026-user-report-templates/SKILL.md) |
| Create from yellow marks | [visa2026-template-scan](../visa2026-template-scan/SKILL.md) |
| Preview slot layout | [visa2026-preview-slot](../visa2026-preview-slot/SKILL.md) |

**Long reference:** [reference.md](./reference.md). **Experience log:** [learnings.md](./learnings.md). **Maturity:** [MATURITY.md](./MATURITY.md).

---

## Scenarios (promoted — check first)

| Symptom | First step | Likely owner |
|---------|------------|--------------|
| Preview still shows the old wording or weight after Sync | Compare master `UserReportTemplate` FileData with the same-name `ApplicationProfileTemplate.TemplateFile`. Sync must publish both | **This skill** |
| Sync SQL fails on PostgreSQL | Quote `"FileData"`, `"Content"`, `"Size"`. Do not use SQL Server `[brackets]` | **This skill** |
| Home **Sync** missing, including on a new template | The button is one Word/Excel add-in, not a control inside each file. One **Edit template** writes `ribbon-host.json` for the folder. If Connect stays false, rebuild the add-in with real `IDTExtensibility2` (`csc /link`) | **This skill** |
| **Install on this PC** throws `ValueManagerContext.Storage is null` | Do not call `SecuritySystem.IsGranted` from that GET. The controller is already `[Authorize]`. The button is shown only in the Blazor circuit | **This skill** |
| Word cannot open a path that contains `ç` or similar | Do not percent-encode the path passed to Word | **This skill** |
| Officer PC has no Visual Studio | Ship `office-sync\Visa2026.OfficeSync.dll`. The installer copies it. Compile only on a machine that has `extensibility.dll` | **This skill** |
| Catalog empty, Check chip, ZIP wrong | Feature domain | **resminamalar** |
| `'{{…}}' could not be replaced` | Merge | **user-report-templates** |

---

## Scope

| In scope | Out of scope |
|----------|----------------|
| Edit template, `%LOCALAPPDATA%\Visa2026\TemplateEdit`, `.meta.json`, `ribbon-host.json` | Resminamalar checkbox selection, readiness chips, ZIP enqueue |
| `visa2026edit:` opener and Office zone trust | DocxTemplater token replacement |
| Word/Excel Home **Sync** (`Visa2026.OfficeSync`) | Baking customUI into each `.docx` / `.xlsx` |
| **Install on this PC** zip | Yellow-marks Approve |
| Ribbon token and `POST staging/ribbon-sync` | Preview-slot resize and theme |

## Rules that keep coming back

- The production server cannot write the officer’s `%LOCALAPPDATA%`. Dev (browser and process on the same PC) writes the folder and starts Office. Production uses the opener.
- **Sync** is the publish step. Saving in Word does not update the database. Catalog **Sync to database** stays.
- Office will not host a third-party button on the built-in Save control. **Sync** is the first group on the **Home** tab.
- Install once per Windows user. The same button covers templates already in the folder and templates added later.
- `Install.cmd` creates `%LOCALAPPDATA%\Visa2026\TemplateEdit`.
- New `.cs` / `.ps1` text is UTF-8 without BOM.
