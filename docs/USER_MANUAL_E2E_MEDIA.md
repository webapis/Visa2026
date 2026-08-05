# User Manual — EasyTest media contract

Status: **v0.4** (screenshots-only — D21)  
Last updated: 2026-08-05

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
| `screenshotsCapturedAt` | ISO-8601 UTC when milestone PNGs were copied from UserManual E2E | pipeline (`Update-UserManualGuideVerification.ps1`) |
| `videoCapturedAt` | ISO-8601 UTC when walkthrough MP4 was captured (same run when applicable) | pipeline |
| `mediaE2eRunId` | E2E screenshot run folder id (e.g. `20260805-124857`) | pipeline — ties to `recordings/screenshots/{id}/` |
| `video` | Optional embed URL | user-manual (after officer review) |

**Validator (Phase 3+):** warn if `e2eScenarioId` set but `scenarios/ready/<id>/` missing.

**Officer site display:** MkDocs hook `hooks/media_capture_labels.py` reads `screenshotsCapturedAt`, `videoCapturedAt`, and `mediaE2eRunId` from page meta and injects:

- **Screenshots** admonition — E2E capture line (UTC + run id)
- **Video walkthrough** caption — same capture line
- **Per-frame caption** under each `assets/screenshots/...` image

Per-file capture detail is also written to `user-manual/assets/screenshots/v{version}/capture-manifest.json` when `Copy-EasyTestManualScreenshots.ps1` runs.

### Doc-anchored capture (canonical — v0.3)

**Principle:** the **guide section** where an image or video appears defines what the media must show. E2E captures that UI state; the registry records the contract; the pipeline copies **1:1** — no fan-out from unrelated journey milestones.

```text
Guide prose + step (officer-visible meaning)
    ↓
<!-- media-capture: {key} --> above the image
    ↓
media-capture-registry.yaml — guideSlugs, description, assertBeforeCapture
    ↓
UserManualMediaCaptureKeys + E2E CaptureAsync at assertion point
    ↓
assets/screenshots/v{version}/{locale}/{key}.png
```

Each screenshot in a **published** guide must declare the capture key on the line **immediately above** the image:

```markdown
<!-- media-capture: navigation-step-01-shell -->
![Application shell with left navigation menu](../../assets/screenshots/v2026.08/en/navigation-step-01-shell.png)
```

| Piece | Rule |
|-------|------|
| **Capture key** | Equals the PNG file stem (`navigation-step-01-shell`) |
| **E2E label** | Same string passed to `PlaywrightScreenshotCapture` / `EasyTestScreenshotCapture` |
| **`guideSlugs`** | One or more guide frontmatter `slug` values where this image is embedded (shared keys allowed when the same UI state is correct in multiple guides) |
| **`description`** | Officer-visible meaning — should match the figure caption / step text |
| **`assertBeforeCapture`** | UI state E2E must satisfy **before** capture (url, visible text, toolbar, field) |
| **Registry** | `user-manual/media-capture-registry.yaml` — source of truth for keys |
| **Copy** | `Copy-EasyTestManualScreenshots.ps1` copies **1:1** (`{key}.png` → `{key}.png`) |
| **Validator** | `Validate-UserManualMediaCaptures.ps1` — published guides: anchor required; key = basename; registry `guideSlugs` must include guide `slug` when `-RequireRegistry` |
| **Pinpoint** | Optional `pinpoint:` in registry; E2E passes Playwright locator to `CaptureAsync` — orange highlight burned into PNG; bbox in `pinpoints.json` |

Constants for the person-officer journey: `Visa2026.E2E.Tests/UserManual/UserManualMediaCaptureKeys.cs`.

#### Pinpoint highlights (action screenshots)

Borrow the **“where to click”** pattern from tools like [Guidde](https://www.guidde.com/gallery/how-to-create-learning-plan-in-docebo) — without external SaaS.

| Rule | Detail |
|------|--------|
| **When** | Toolbar buttons, nav items, tabs, primary fields — not full-shell overview shots |
| **How** | `CaptureAsync(page, key, locator)` → `UserManualScreenshotPinpoint` draws ring + pointer on PNG |
| **Registry** | `pinpoint: { kind, label \| titlePrefix \| cssClass }` documents intent |
| **Opt out** | `VISA2026_E2E_PINPOINTS=false` |
| **Manifest** | `recordings/screenshots/{runId}/pinpoints.json` merged into `capture-manifest.json` |

Overview captures (`navigation-step-01-shell`, `login-step-02-report-dashboard`) omit pinpoint.

**Workflow (create or update a guide):**

1. Write the step prose and decide what the figure must show.
2. Add `<!-- media-capture: {key} -->` + `![...](.../{key}.png)`.
3. Add or update a registry row (`guideSlugs`, `description`, `assertBeforeCapture`).
4. Add the key to `UserManualMediaCaptureKeys.cs` and call `CaptureAsync` after the assertions in E2E.
5. Run `Build-UserManual.ps1` (or E2E + copy scripts) — verify PNG matches the prose.

#### Legacy fan-out (deprecated)

`Copy-EasyTestManualScreenshots.ps1` still maps old milestone labels (`00-logon-page`, `04-employee-detail`, …) to **many** destination PNGs for guides **not yet migrated** to doc-anchored keys. That reuse causes screenshots that do not match guide prose.

| Status | Guides |
|--------|--------|
| **Doc-anchored** | Pilots 1–5 (`getting-started/login`, `getting-started/navigation`, `person/open-and-search`, `employee/register`, `employee/add-passport`) |
| **Legacy fan-out** | All other guides with screenshots until migrated per guide |

**Do not add new fan-out mappings.** Migrate guides to dedicated capture keys instead.

#### Video (deferred — D21)

**Officer manual publish is screenshots-only.** Do not add `<video>` blocks or `video*` frontmatter to guides.

| Policy | Detail |
|--------|--------|
| **Default** | UserManual Playwright E2E captures **PNG only** (`VISA2026_E2E_VIDEO_RECORDING` off) |
| **Guides** | Step prose + doc-anchored screenshots; no Video walkthrough section |
| **CI** | `Validate-UserManualMediaCaptures.ps1` fails published guides with `<video>` or video frontmatter |
| **Optional infra** | `Copy-EasyTestManualVideos.ps1`, registry `videos:`, `-EnableVideo` on `Record-PlaywrightE2e.ps1` — retained for future experiments, not officer publish |

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

### Phase 3 — automated capture (in progress)

Doc-anchored keys ship in **`UserManualMediaCaptureKeys.cs`** + `PlaywrightScreenshotCapture.CaptureAsync(page, key)`.

| Piece | Path |
|-------|------|
| Keys | `Visa2026.E2E.Tests/UserManual/UserManualMediaCaptureKeys.cs` |
| Capture | `PlaywrightScreenshotCapture` / `EasyTestScreenshotCapture` at registry assertion point |
| Copy script | `scripts/ci/Copy-EasyTestManualScreenshots.ps1` (1:1 for doc keys) |
| Destination | `user-manual/assets/screenshots/v{version}/{locale}/{key}.png` |

**Naming:** `{topic}-step-{NN}-{short-label}` — key equals PNG stem (e.g. `person-register-step-02-saved-detail`).

Call capture only after `assertBeforeCapture` conditions in the registry are satisfied in the test.

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

**Invariant:** EasyTest **source** recordings stay in `Visa2026.E2E.Tests/recordings/` (gitignored). **Screenshots** under `user-manual/assets/screenshots/**/*.png` are **committed** (D22) for GitHub Pages. **Videos** under `user-manual/assets/videos/` remain gitignored (D21). Regenerate PNGs via `Record-PlaywrightE2e.ps1` + `Copy-EasyTestManualScreenshots.ps1`, then commit; CI uses `Build-UserManual.ps1 -RequireMedia`.

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
| 2026-08-05 | **v0.3** — doc-anchored capture canonical; `guideSlugs` in registry; legacy fan-out deprecated |
| 2026-08-05 | Video doc-anchoring at guide level (`videoFile`, optional `videoCaptureKey`) |
| 2026-08-05 | **D21** — screenshots-only officer manual; video deferred |
