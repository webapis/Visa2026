# User manual — reference

Canonical plan: [`docs/USER_MANUAL_IMPLEMENTATION_PLAN.md`](../../../docs/USER_MANUAL_IMPLEMENTATION_PLAN.md).

**Advise first:** [advisory.md](./advisory.md) — when to document, path options, pre-flight questions.

**Roadmap:** [`docs/USER_MANUAL_ROADMAP.md`](../../../docs/USER_MANUAL_ROADMAP.md)

**E2E media:** [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) · producer skill [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md)

**Live status:** [tracking.md](./tracking.md) (phases, guide inventory, doc debt, CI).

**Locales:** [localization.md](./localization.md) — **en, tr, tk, ru**.

**Officer content rules:** [content-policy.md](./content-policy.md) — **no code on the published site**.

**Maintainer note:** C# / YAML examples below are for **generator and CI authors only** — never copy into `user-manual/docs/`.

**Status:** paths below are **planned** until Phase 0–1 land in the repo.

---

## Repository layout

```text
user-manual/
  mkdocs.yml
  requirements.txt              # includes mkdocs-static-i18n
  docs/
    en/
      index.md
      getting-started/
      guides/
        _template.md
      reference/
    tr/
    tk/
    ru/
  assets/screenshots/v{yyyy.MM}/{en|tr|tk|ru}/
  generated/
    bo-catalog.json
    navigation-tree.json

tools/UserManualManifestGenerator/
  Program.cs
  Readers/

tools/UserManualManifestGenerator.Tests/
  BoCatalogGeneratorTests.cs
  GuideManifestParityTests.cs

Visa2026.Module.Tests/Documentation/   # optional: [UserDocumentation] on BOs
  UserDocumentationAttributeTests.cs

Visa2026.Module/Documentation/
  UserDocumentationAttribute.cs

scripts/ci/
  Build-UserManual.ps1
  Validate-UserManualLinks.ps1
  Publish-UserManualPages.ps1

.github/workflows/
  user-manual.yml
```

**Gitignore contract:** [§ Gitignore](#gitignore-contract) below — authoritative list is repo [`.gitignore`](../../../.gitignore) lines ~490–510.

---

## Gitignore contract

**Rule:** officer **prose**, **generator JSON**, and **promoted screenshots** are committed; **build output**, **local tooling**, **videos**, and **test report runs** are not.

Do **not** `git add` ignored paths “to fix CI” — regenerate with `Build-UserManual.ps1` / `Serve-UserManual.ps1` / E2E copy scripts instead.

### Ignore (never commit)

| Path | Role |
|------|------|
| `user-manual/site/` | MkDocs HTML output |
| `user-manual/.tools/` | Portable Python from `Serve-UserManual.ps1` |
| `user-manual/.mkdocs_cache/` | MkDocs cache |
| `user-manual/user-manual/` | Accidental nested MkDocs output |
| `user-manual/docs/assets/` | Build-time media sync into docs tree |
| `user-manual/docs/*/reference/business-objects.md` | Build-time copy of generated reference page |
| `user-manual/assets/videos/**/*.{mp4,webm,mov}` | Promoted E2E video (deferred; D21) |
| `user-manual/manual-media.env` | Local env override (keep `manual-media.env.example` tracked) |
| `deploy/manual/` | Published site + media bundle on server/agent |
| `manual-test-reports/latest/` | Generated green-tick / summary JSON+HTML |
| `manual-test-reports/runs/` | Archived TRX per pipeline run |
| `TestResults/` | xUnit output (`UserManualDocs`, E2E) |
| `pw-test-out.txt` | Playwright log scratch |
| `playwright-report/`, `test-results/` | Playwright artifacts when recording manual E2E |

**E2E upstream** (visa2026-easytest-e2e): `Visa2026.E2E.Tests/recordings/`, `.tools/`, `.webdrivers/` — raw captures before `Copy-EasyTestManual*.ps1`.

### Commit (source of truth)

| Path | Role |
|------|------|
| `user-manual/docs/**` | Guide prose (en/tr/tk/ru) — except ignored copies above |
| `user-manual/mkdocs.yml`, `requirements.txt`, `hooks/` | Site config |
| `user-manual/generated/bo-catalog.json` | Layer A catalog |
| `user-manual/generated/navigation-tree.json` | Nav tree from generator |
| `user-manual/generated/reference/**/business-objects.md` | Generator output (reviewed; synced to docs at build) |
| `user-manual/assets/screenshots/**/*.png` | Promoted E2E screenshots (**D22** — GitHub Pages + clone-and-build) |
| `user-manual/media-capture-registry.yaml` | Doc-anchored capture keys |
| `manual-test-reports/manifest.yaml`, `README.md` | Suite registry (not generated output) |
| `.cursor/skills/visa2026-user-manual/**` | This skill |

### Agent anti-patterns

- Committing `user-manual/site/` or `.tools/` — bloats PRs; CI builds its own site
- Committing video under `user-manual/assets/videos/` — screenshots-only policy (D21)
- Deleting `.gitkeep` files in empty screenshot version folders — they document expected layout
- Publishing guides that reference PNGs not yet copied from E2E — run Record + `Copy-EasyTestManualScreenshots.ps1`, then commit PNGs before merge (CI uses `-RequireMedia`)

**GitHub Pages:** `user-manual.yml` on `master` deploys `user-manual/site/` with `--site-url https://<owner>.github.io/<repo>/`. Enable **Settings → Pages → GitHub Actions** once per repo.

Canonical on-prem deploy of ignored artifacts (videos, remote media URL): [`docs/USER_MANUAL_RELEASE.md`](../../../docs/USER_MANUAL_RELEASE.md).

---

## Commands

### Full manual build (Phase 1+)

```powershell
# From repo root
./scripts/ci/Build-UserManual.ps1
```

Runs in order: **`UserManualDocs` unit tests** → generator → link validator → **`UserManual` E2E** → mkdocs.

Filter unit tests only:

```powershell
dotnet test tools/UserManualManifestGenerator.Tests --filter "Category=UserManualDocs"
```

Equivalent steps:

```powershell
dotnet build Visa2026.slnx -c Debug
dotnet run --project tools/UserManualManifestGenerator -- `
  --module Visa2026.Module/bin/Debug/net8.0/Visa2026.Module.dll `
  --output user-manual/generated
./scripts/ci/Validate-UserManualLinks.ps1
pip install -r user-manual/requirements.txt
mkdocs build -f user-manual/mkdocs.yml -d user-manual/site
```

### Local preview

**Default:** run the repo script — do **not** hand-install Python or pip unless debugging a script failure.

```powershell
# From repo root — builds (SkipE2E) + mkdocs serve + hot reload
./scripts/local/Serve-UserManual.ps1

# Reopen only (reuse user-manual/site/ and user-manual/.tools/)
./scripts/local/Serve-UserManual.ps1 -SkipBuild

# Custom port; no browser launch
./scripts/local/Serve-UserManual.ps1 -Port 9000 -NoBrowser
```

| Item | Detail |
|------|--------|
| **URL** | http://127.0.0.1:8765/manual/ (not port 8000) |
| **Hot reload** | `mkdocs serve` (no `--dirtyreload` — dirty reload leaves unread pages as `None` in nav) — edit `user-manual/docs/` |
| **Python reuse** | System `python` / `py -3` if found; else **`user-manual/.tools/python312/`** (portable embed, **keep between runs**) |
| **Requirements** | Script runs `pip install -r user-manual/requirements.txt` into that interpreter |
| **Build step** | `Build-UserManual.ps1 -SkipE2E` unless `-SkipBuild` |
| **Remote media** | `-ManualMediaBaseUrl 'https://host:8082/manual-media'` — loads PNGs from nginx instead of local `user-manual/assets/` |

**Do not** (common agent mistakes):

- `pip install -r user-manual/requirements.txt` as a standalone first step when the script suffices
- Delete or re-download `user-manual/.tools/` on every preview request
- Bare `mkdocs serve -f user-manual/mkdocs.yml` (wrong port/path; skips build/sync)

**Not Blazor:** preview does not require `dotnet run` unless recording new E2E screenshots.

**Deploy (static beside app):** [`docs/USER_MANUAL_RELEASE.md`](../../../docs/USER_MANUAL_RELEASE.md).

### Publish (Phase 3+, main branch)

```powershell
./scripts/ci/Publish-UserManualPages.ps1
```

Pattern follows `scripts/ci/Prepare-UiScenarioPagesPublish.ps1` (versioned + `latest` on gh-pages).

---

## `[UserDocumentation]` attribute

```csharp
namespace Visa2026.Module.Documentation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UserDocumentationAttribute : Attribute
{
    public string Slug { get; }
    public string? Category { get; init; }
    public UserDocumentationAttribute(string slug) => Slug = slug;
}
```

**Pilot BOs (Phase 1):** `Person`, `Application`, `ApplicationItem`, `ApplicationProgress`.

Apply on **feature anchors**, not every lookup table.

---

## Guide frontmatter (required)

```yaml
---
title: Register a new employee
slug: person/register
bo: Person
relatedBo: [Passport, Education]
navPath: Employee
roles: [Visa Officer]
guideStatus: draft   # draft | review | published (not `status` — reserved by MkDocs Material for nav badges)
screenshotsVersion: "2026.08"
locale: en   # en | tr | tk | ru — must match folder docs/{locale}/
video: https://youtu.be/xxxxxxxx
e2eScenarioId: person-employee-create
verified: false                 # pipeline sets true after UserManual E2E pass
verifiedAt:                      # ISO-8601 — auto
verifiedCommit:                  # short sha — auto
sourceDocs:
  - docs/PERSON_INCOMPLETE_DATA.md
  - docs/PERSON_DETAIL_NESTED_COLLECTION_TABS.md
---
```

| Field | Rule |
|-------|------|
| `slug` | Unique; same across all locales; matches `[UserDocumentation]` when overview |
| `locale` | **Required** — `en`, `tr`, `tk`, or `ru`; path `docs/{locale}/guides/...` |
| `bo` | Must exist in `bo-catalog.json` (usually `Person` until per-BO catalog pages ship) |
| `personRole` | Optional — `Employee`, `FamilyMember`, `TemporaryVisitor`; must match typed detail view when set |
| `navPath` | Optional — sidebar grouping hint (`Employee`, `FamilyMember`) |
| `status` | Only `published` after officer review |
| `screenshotsVersion` | Folder must exist under `assets/screenshots/` (warn if missing) |
| `screenshotsCapturedAt` | ISO-8601 UTC — pipeline sets when milestone PNGs copied from UserManual E2E |
| `videoCapturedAt` | _Deprecated (D21)_ — omit on new guides |
| `mediaE2eRunId` | E2E screenshot run folder id — ties media to `recordings/screenshots/{id}/` |
| `e2eScenarioId` | Folder `Visa2026.E2E.Tests/scenarios/ready/<id>/` when set |
| `verified` | **`true` only** when set by `Build-UserManual.ps1` — shows green tick on site |
| `verifiedAt` / `verifiedCommit` | Auto from test report — not hand-edited |
| `e2eTestFilter` | Maintainer/CI only — not shown on officer site |
| `video` / `videoFile` / `videoCaptureKey` | **Do not use** — screenshots-only (D21) |
| `sourceDocs` | Traceability for AI; not shown on site; **do not quote in guide body** |

---

## Officer guide body (published content)

Follow [content-policy.md](./content-policy.md). Summary:

- Write steps with **menu paths** and **on-screen labels** from catalog `displayName`.
- Use screenshots/video — not code blocks or BO/property names.
- No links to `docs/` developer pages.

---

## Catalog JSON (generator output)

```json
{
  "generatedAt": "2026-08-04T12:00:00Z",
  "assemblyVersion": "1.0.0.0",
  "types": [
    {
      "name": "Person",
      "fullName": "Visa2026.Module.BusinessObjects.Person",
      "displayName": "Person",
      "navigationPath": null,
      "userDocSlug": "person/overview",
      "userDocCategory": "Person management",
      "properties": [
        {
          "name": "FirstName",
          "displayName": "First name",
          "required": true,
          "hiddenWhen": "PersonRole != Employee"
        }
      ],
      "actions": [],
      "guideSlugs": ["person/register"]
    }
  ]
}
```

**Generator inputs (reflection):**

- `[NavigationItem]`, `[XafDisplayName]`, `[DisplayName]`
- `[RuleRequiredField]`, `[Appearance]` (visibility summary)
- `[UserDocumentation]`
- Optional: controller actions with officer-visible captions

**`guideSlugs`:** filled by scanning `user-manual/docs/guides/*.md` frontmatter in validator.

---

## CI validator rules

PowerShell checks below are complemented by **`[Trait("Category", "UserManualDocs")]`** xUnit tests (generator output, manifest parity, catalog invariants). See [`USER_MANUAL_PIPELINE.md` §5.1](../../../docs/USER_MANUAL_PIPELINE.md).

| Check | Severity |
|-------|----------|
| Guide `bo:` not in catalog | **Fail** |
| Duplicate guide `slug:` | **Fail** |
| Broken internal Markdown links | **Fail** |
| Fenced code block in `guides/` or `reference/` body | **Fail** ([content-policy.md](./content-policy.md)) |
| Test output, TRX, or `dotnet test` text in guide body | **Fail** — use green tick only ([testing-evidence.md](./testing-evidence.md)) |
| `verified: true` without pipeline `summary.json` for same commit | **Fail** |
| Link to `docs/` developer page from officer content | **Warn** → fail Phase 2 |
| `[UserDocumentation]` with no guide/reference | **Warn** (fail after Phase 2) |
| Missing `screenshotsVersion` asset folder | **Warn** |
| `video:` URL unreachable | **Warn** (optional) |

---

## MkDocs Material (`mkdocs.yml` sketch)

```yaml
site_name: Visa2026 User Manual
site_url: https://example.github.io/Visa2026/manual/
theme:
  name: material
  features:
    - navigation.tabs
    - navigation.sections
    - search.suggest
    - content.code.copy
nav:
  - Home: index.md
  - Getting started:
      - getting-started/login-and-roles.md
      - getting-started/daily-workflow-overview.md
  - How-to guides:
      - guides/person-register.md
  - Reference:
      - reference/index.md
  - Administration:
      - administration/user-report-templates.md
plugins:
  - search
```

Extend `nav` as guides ship. Reference section may be partially generated in CI.

---

## Screenshot conventions (doc-anchored)

**Canonical:** [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) § Doc-anchored capture.

| Piece | Convention |
|-------|------------|
| Anchor | `<!-- media-capture: {key} -->` on the line **above** each `![...](assets/screenshots/...)` |
| Key | Equals PNG basename without extension |
| Registry | `user-manual/media-capture-registry.yaml` — `guideSlugs`, `description`, `assertBeforeCapture` |
| E2E | `UserManualMediaCaptureKeys.{Key}` → `CaptureAsync(page, key)` after assertions |
| Path | `assets/screenshots/v2026.08/{en\|tr\|tk\|ru}/{key}.png` |
| Copy | `Copy-EasyTestManualScreenshots.ps1` — 1:1 for doc keys; legacy fan-out **deprecated** |
| Validate | `Validate-UserManualMediaCaptures.ps1` in `Build-UserManual.ps1` |

**Workflow:** prose → anchor → registry → E2E key → pipeline. Never add a screenshot without all four.

**Markdown example:**

```markdown
<!-- media-capture: person-register-step-02-saved-detail -->
![Employee detail after Save](../assets/screenshots/v2026.08/en/person-register-step-02-saved-detail.png)
*Figure: New employee saved — note the assigned Personal Number.*
```

---

## Screenshot conventions (legacy — unmigrated guides only)

| Piece | Convention |
|-------|------------|
| Path | `assets/screenshots/v2026.08/{en\|tr\|tk\|ru}/person-register-step-03.png` |
| Markdown | `![Step 3](../assets/screenshots/v2026.08/en/person-register-step-03.png)` |
| Copy | `Copy-EasyTestManualScreenshots.ps1` legacy `$map` fan-out from milestone labels |
| Baseline update | PR review when CI refreshes PNGs |

**Migrate to doc-anchored** before adding new images to a guide.

---

Playback markup depends on the **Phase 3** storage choice ([USER_MANUAL_E2E_MEDIA.md](../../../docs/USER_MANUAL_E2E_MEDIA.md) §5.1).

**Embed (option A) — example:**

```markdown
<iframe width="560" height="315" src="https://www.youtube.com/embed/VIDEO_ID"
  title="Visa2026 — Register an employee (v2026.08)" frameborder="0"
  allowfullscreen></iframe>
```

**Static file (option B) — example:**

```markdown
<video controls src="../assets/videos/v2026.09/en/person-register.mp4"></video>
```

**App stream (option D) — example:** link or iframe to `UserManualBaseUrl` + slug when Phase 5 ships.

Frontmatter: `video`, `videoStorage` (`tbd` until decided), `videoSource` (EasyTest path).

---

## Information architecture (sidebar)

```text
Getting started → How-to guides (by task) → Reference (from catalog) → Administration → Release notes
```

Reference nav tree mirrors `[NavigationItem("Lookup/Visa")]` paths from catalog — replaces hand-maintained `LookupNavigationStructure.md` when generator ships.

---

## Open decisions (see plan §15)

| # | Topic | Recommendation |
|---|--------|----------------|
| 1 | Commit `bo-catalog.json` | Yes, on main |
| 2 | Commit generated `reference/*.md` | **Yes** under `user-manual/generated/reference/`; **no** under `docs/*/reference/` (build copy, gitignored) |
| 3 | Hosting | Internal first if screenshots sensitive |
| 4 | **Locales** | **en, tr, tk, ru** — see [localization.md](./localization.md) |
| 5 | `LookupNavigationStructure.md` | Deprecate when nav tree generated |
| 6 | Video storage backend | **Open** — Phase 3; see E2E media doc §5.1 |
