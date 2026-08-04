# User Manual — EasyTest media contract

Status: **Draft v0.1**  
Last updated: 2026-08-04

**Skills:** [visa2026-user-manual](../.cursor/skills/visa2026-user-manual/SKILL.md) · [visa2026-easytest-e2e](../.cursor/skills/visa2026-easytest-e2e/SKILL.md)

**Roadmap:** [USER_MANUAL_ROADMAP.md](USER_MANUAL_ROADMAP.md)

**Pipeline (E2E embedded, not separate):** [USER_MANUAL_PIPELINE.md](USER_MANUAL_PIPELINE.md)

This document is the **media contract** between officer manual generation and EasyTest E2E. **Orchestration** (single `Build-UserManual.ps1`) is defined in the pipeline doc.

---

## 1. Why EasyTest is the media source

EasyTest runs **inside** [USER_MANUAL_PIPELINE.md](USER_MANUAL_PIPELINE.md) — not as a separate step before manual build.

| Approach | Problem |
|----------|---------|
| Separate E2E runner, manual updated later | Screenshots/video drift; officers see wrong UI |
| Ad-hoc desktop screenshots | Unreproducible; wrong DB |
| Production screenshots | PII risk |
| **EasyTest in `Build-UserManual.ps1`** | Same run proves journeys + produces PNG/MP4 |

EasyTest already provides (media **ON by default** for documentation generation):

- **Video:** `scripts/local/Record-EasyTest.ps1` (ffmpeg **Edge window** by default, `-RecordTarget Desktop` for full screen; `-NoRecord` to skip) + CI long-run `easytest-e2e-recording` artifact
- **Screenshots:** `EasyTestScreenshotCapture` milestone PNGs under `recordings/screenshots/` (**default ON**; opt out `VISA2026_E2E_SCREENSHOTS=false` / `-NoScreenshots`) + CI `easytest-e2e-screenshots` artifact
- **Failure dumps:** `EasyTestBlazorNavigationHelper.TryDumpDiagnostics` (post-mortem PNG)
- **Guide promotion:** `Copy-EasyTestManualScreenshots.ps1` → `user-manual/assets/` (Phase 3 polish: `UserManualMediaCapture`)

---

## 2. Identifiers (link guide ↔ E2E)

| ID | Where | Example |
|----|-------|---------|
| **Guide `slug`** | `user-manual/docs/guides/*.md` frontmatter | `person/register` |
| **`e2eScenarioId`** | Guide frontmatter + `scenarios/ready/<id>/` folder | `person-employee-create` |
| **E2E test id** | `*_map.md` §0, `docs/TESTING_PLAN.md` | `E2E-010` |
| **`screenshotsVersion`** | Guide frontmatter + asset folder | `2026.09` |
| **Screenshot file** | `assets/screenshots/v{version}/{locale}/` | `person-register-step-03-employees-list.png` |

### Guide frontmatter (E2E fields)

```yaml
---
title: Register a new employee
slug: person/register
bo: Person
e2eScenarioId: person-employee-create
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
screenshotsVersion: "2026.09"
video: https://youtu.be/xxxxxxxx   # after Record-EasyTest + upload
videoSource: recordings/person-register.mp4
status: draft
---
```

| Field | Required | Owner skill |
|-------|----------|-------------|
| `e2eScenarioId` | When E2E exists | easytest-e2e creates scenario; user-manual references |
| `e2eTestFilter` | When video auto-recorded | easytest-e2e |
| `screenshotsVersion` | When guide has images | user-manual |
| `video` | Optional embed URL | user-manual (after officer review) |

**Validator (Phase 3+):** warn if `e2eScenarioId` set but `scenarios/ready/<id>/` missing.

---

## 3. Scenario map alignment

EasyTest **Option A** workflow ([reference-map-contract.md](../.cursor/skills/visa2026-easytest-e2e/reference-map-contract.md)):

```text
scenarios/<id>_map.md  →  caption inventory (§3) = officer-visible labels for guide
scenarios/<id>.yaml    →  step list = guide step order
*Tests.cs              →  executable journey
guide.md               →  prose + screenshots + video embed
```

When writing a guide:

1. Read the promoted `*_map.md` **§3 Caption inventory** — use those strings in steps.
2. Do not paraphrase field captions differently from the map/E2E parameters.
3. Add business context the map does not cover (roles, when to use, approvals).

---

## 4. Screenshots

### Phase 2 — manual capture

Officer or tech writer captures PNG while running headed EasyTest locally:

```powershell
# Preferred — video + screenshots ON by default:
.\scripts\local\Record-EasyTest.ps1 `
  -Filter 'PersonOfficerJourney_LoginCreateEmployeeAddPassport'

# Bare test still captures milestone PNGs (no ffmpeg MP4):
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest `
  --filter "FullyQualifiedName~PersonOfficerJourney_LoginCreateEmployeeAddPassport"
```

Save to:

```text
user-manual/assets/screenshots/v2026.09/en/person-register-step-NN-<short-label>.png
```

### Phase 3 — automated capture (planned)

**New helper** (E2E.Tests): `UserManualMediaCapture.CaptureStep(slug, stepKey, outputDir)`

| Piece | Path |
|-------|------|
| Helper | `Visa2026.E2E.Tests/UserManualMediaCapture.cs` |
| Output (CI artifact) | `Visa2026.E2E.Tests/manual-media/{slug}/{stepKey}.png` |
| Copy script | `scripts/ci/Copy-EasyTestManualScreenshots.ps1` |
| Destination | `user-manual/assets/screenshots/v{version}/{locale}/` |

**Naming:** `{slug-with-dashes}-step-{NN}-{stepKey}.png`  
**Example:** `person-register-step-02-employees-list.png`

Call capture after stable navigation assertions (`NavigateEmployeesList`, `AssertEmployeeDetailViewActive`).

### Markdown reference in guide

```markdown
![Employees list](../assets/screenshots/v2026.09/en/person-register-step-02-employees-list.png)
*Figure: Employees list — Personal Number **E2E-EMP-010** is test data.*
```

---

## 5. Video tutorials

**Video storage backend: open (TBD in Phase 3).** Production MP4 must not live in **git** source; see [§5.1 Storage options](#51-video-storage-options-open). Guide frontmatter stays storage-agnostic until a option is chosen.

### 5.1 Video storage options (open)

| Option | Playback | Pros | Cons | When to pick |
|--------|----------|------|------|--------------|
| **A — Embed URL** | YouTube / SharePoint / Vimeo iframe in MkDocs | Simple static site; streaming built-in | Public-repo / hosting policy; embed dependency | External manual, no in-app player |
| **B — Static file beside manual** | `user-manual/assets/videos/` or nginx/IIS static path | On-prem friendly; no DB | Large deploy; no XAF permissions | Internal GitHub Pages / company static host |
| **C — Object / file share** | URL to MinIO, S3, UNC share | Scales; DB stays small | Extra infra | On-prem with existing file server |
| **D — PostgreSQL / `FileData`** | Stream via Blazor/API endpoint | XAF security; same pattern as scans | DB size, streaming code, not for MkDocs alone | In-app Help (Phase 5) + role-gated video |
| **E — Hybrid** | Metadata in Postgres; bytes in B or C | Flexible | Two systems to operate | Large catalog, mixed audiences |

**Decision gate (Phase 3):** product + IT choose A–E per environment (dev demo vs on-prem prod). Document the winner in [tracking.md](../.cursor/skills/visa2026-user-manual/tracking.md) open decisions and update guide `videoStorage` frontmatter.

**Invariant (all options):** EasyTest **source** recordings stay in `Visa2026.E2E.Tests/recordings/` (gitignored). Promoted PNG/MP4 under `user-manual/assets/screenshots/` and `user-manual/assets/videos/` are also **gitignored** — generate via `Record-EasyTest.ps1` + `Copy-EasyTestManual*.ps1` before local preview or run `Build-UserManual.ps1 -RequireMedia` after media copy.

### Local record

```powershell
.\scripts\local\Record-EasyTest.ps1 `
  -Filter 'PersonOfficerJourney_LoginCreateEmployeeAddPassport' `
  -OutputName 'person-register.mp4'
```

Output: `Visa2026.E2E.Tests/recordings/person-register.mp4` and `recordings/screenshots/{run}/` (gitignored). Both ON by default.

### CI record

Long runs (`schedule`, `workflow_dispatch`, `push` to master) upload **`easytest-e2e-recording`** artifact (`officer-journey.mp4`).

### Publish to manual (storage TBD)

1. Trim/silence in editor if needed (optional).
2. Promote MP4 to the **chosen store** (§5.1) — embed URL, static path, object URL, or `FileData` + stream endpoint.
3. Set guide frontmatter (see below); note `videoSource` for traceability.
4. Title convention: `Visa2026 — Register an employee (v2026.09)`.

### Guide frontmatter — video fields (storage-agnostic)

```yaml
video: https://…                    # embed URL OR app stream URL OR static HTTPS path
videoStorage: embed                 # embed | static | object | filedata | hybrid (when decided)
videoSource: recordings/person-register.mp4
videoObjectKey: …                   # optional — S3/UNC key when using object storage
```

Until Phase 3 decision: leave `videoStorage` unset or `tbd`; `video` may be empty while guide is `draft`.

---

## 6. Workflow: new guide with media

```mermaid
sequenceDiagram
  participant O as Officer / Product
  participant E2E as visa2026-easytest-e2e
  participant UM as visa2026-user-manual
  O->>E2E: Define journey + E2E-xxx id
  E2E->>E2E: map.md + yaml + *Tests.cs → scenarios/ready/
  E2E->>E2E: CI green; Record-EasyTest.mp4
  UM->>UM: guide draft from map §3 + catalog
  E2E->>UM: PNGs (manual or CaptureStep)
  UM->>UM: assets + frontmatter e2eScenarioId
  O->>UM: review → status published
  UM->>UM: mkdocs build + Pages deploy
```

| Step | Skill |
|------|-------|
| 1. Scenario promoted | easytest-e2e |
| 2. Guide draft | user-manual |
| 3. Screenshots | easytest-e2e produces → user-manual wires |
| 4. Video record | easytest-e2e → user-manual embed |
| 5. Officer sign-off | user-manual `tracking.md` |
| 6. learnings.md | **both** skills append relevant notes |

---

## 7. Pilot mapping table

| Guide slug | `e2eScenarioId` | Test filter (video) | Screenshot steps (planned) |
|------------|-----------------|----------------------|----------------------------|
| `person/register` | `person-employee-create` | `PersonOfficerJourney_LoginCreateEmployeeAddPassport` | login, employees list, detail, save |
| `applications/create` | _TBD_ | _TBD_ | _TBD_ |
| `applications/add-items` | _TBD_ | _TBD_ | _TBD_ |
| `applications/document-copies` | _TBD_ | _TBD_ | _TBD_ |
| `person/dossier` | _TBD_ | _TBD_ | _TBD_ |

Update this table when scenarios ship.

---

## 8. Ownership when things break

| Symptom | Fix in |
|---------|--------|
| E2E navigation wrong | easytest-e2e |
| Caption fill fails | Module/Blazor + easytest-e2e |
| Guide step text wrong | user-manual (from map + catalog) |
| Screenshot missing / stale | easytest-e2e capture + user-manual assets |
| Video embed broken | user-manual frontmatter |
| `e2eScenarioId` orphan | add scenario or remove frontmatter field |

---

## 9. Changelog

| Date | Change |
|------|--------|
| 2026-08-04 | Initial contract v0.1 |
| 2026-08-04 | Video storage options A–E documented; backend TBD Phase 3 |
| 2026-08-04 | E2E embedded in unified pipeline ([USER_MANUAL_PIPELINE.md](USER_MANUAL_PIPELINE.md)) |
