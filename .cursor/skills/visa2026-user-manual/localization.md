# User manual — localization (en · tr · tk · ru)

**Supported officer manual languages:** **English**, **Turkish**, **Turkmen**, **Russian**.

Aligned with app UI locales in [`docs/LOCALIZATION_PLAN.md`](../../../docs/LOCALIZATION_PLAN.md) (Layer A). The manual site uses the same four languages; **default locale is English (`en`)**.

**Skill:** [SKILL.md](./SKILL.md) · **Content rules:** [content-policy.md](./content-policy.md)

---

## 1. Locale matrix

| Language | BCP-47 | MkDocs folder | Screenshot subfolder | App UI culture (target) |
|----------|--------|---------------|----------------------|-------------------------|
| English | `en` | `docs/en/` | `assets/screenshots/v{ver}/en/` | `en` |
| Turkish | `tr` | `docs/tr/` | `assets/screenshots/v{ver}/tr/` | `tr` |
| Turkmen | `tk` | `docs/tk/` | `assets/screenshots/v{ver}/tk/` | `tk` or `tk-TM` |
| Russian | `ru` | `docs/ru/` | `assets/screenshots/v{ver}/ru/` | `ru` |

**Site default:** `en` — matches app default in `LOCALIZATION_PLAN.md`.

**Officer rule:** each published guide exists in **all four locales** before the guide is considered fully shipped (per-locale `status` may still be `draft` until reviewed).

---

## 2. Repository layout

```text
user-manual/
  mkdocs.yml                    # mkdocs-static-i18n plugin
  requirements.txt
  docs/
    en/
      index.md
      getting-started/
      guides/
        person/register.md
      reference/
    tr/                           # same slug paths as en
    tk/
    ru/
  assets/
    screenshots/v2026.08/
      en/
      tr/
      tk/
      ru/
  generated/
    bo-catalog.json               # en display names baseline
    bo-catalog.tr.json            # Phase 4+ when model aspects exist
    bo-catalog.tk.json
    bo-catalog.ru.json
```

**Slug parity:** `person/register` must exist at:

- `docs/en/guides/person/register.md`
- `docs/tr/guides/person/register.md`
- `docs/tk/guides/person/register.md`
- `docs/ru/guides/person/register.md`

Same `slug` in frontmatter; `locale` matches folder.

---

## 3. MkDocs Material + i18n

**Plugin:** [mkdocs-static-i18n](https://github.com/ultrabug/mkdocs-static-i18n) (add to `requirements.txt` in Phase 0).

```yaml
plugins:
  - search
  - i18n:
      docs_structure: folder
      languages:
        - locale: en
          name: English
          default: true
          build: true
        - locale: tr
          name: Türkçe
          build: true
        - locale: tk
          name: Türkmen
          build: true
        - locale: ru
          name: Русский
          build: true
      nav_translations:
        tr:
          Home: Ana sayfa
        tk:
          Home: Baş sahypa
        ru:
          Home: Главная
```

Language switcher appears in the Material theme header. **Phase 0:** scaffold all four locales; `en` has placeholder pages; `tr`/`tk`/`ru` may show “Translation in progress” until content lands.

---

## 4. Guide frontmatter (per locale file)

```yaml
---
title: Register a new employee          # translated per file
slug: person/register                   # same across locales
locale: en                              # en | tr | tk | ru
bo: Person                              # catalog key — not translated
status: draft                           # per locale — can publish en before tk
screenshotsVersion: "2026.08"
roles: [Visa Officer]
e2eScenarioId: person-employee-create   # shared journey id
---
```

| Field | Localized? |
|-------|------------|
| `title`, body prose | **Yes** — human or approved glossary |
| `slug`, `bo`, `e2eScenarioId` | **No** — stable keys |
| `status` | **Per locale** — `en` can be `published` while `tk` is `draft` |
| Screenshots | **Per locale** — UI labels must match that language |

---

## 5. Content and translation rules

| Rule | Detail |
|------|--------|
| **No code in any locale** | [content-policy.md](./content-policy.md) applies to tr/tk/ru |
| **UI labels from catalog** | Use `displayName` for that culture when generator supports it |
| **No machine translation (v1)** | Same as `LOCALIZATION_PLAN.md` P6 — officer or approved translator |
| **English first for new guides** | Draft `en` → review → then tr/tk/ru in same PR or follow-up PR |
| **Do not mix languages** in one file | One locale per Markdown file |

**Adapting English draft to tr/tk/ru:** Cursor Agent with `@visa2026-user-manual` + this file; officer reviewer **per language** before `status: published`.

---

## 6. Screenshots and E2E

| Phase | Screenshots |
|-------|-------------|
| **Before app UI i18n ships** | `en` only; other locales use en image + note “UI shown in English” (temporary) |
| **After `PreferredCulture` works** | EasyTest sets culture per run; `UserManualMediaCapture` writes to `assets/.../{locale}/` |

**Manifest** (`manual-generation-manifest.yaml`):

```yaml
guides:
  - slug: person/register
    locales: [en, tr, tk, ru]
    screenshots:
      - stepKey: employees-list
        files:
          en: person-register-step-02-employees-list.png
          tr: person-register-step-02-employees-list.png
          tk: person-register-step-02-employees-list.png
          ru: person-register-step-02-employees-list.png
```

Validator (Phase 2+): `published` + `locale: en` requires sibling files for tr/tk/ru **or** explicit `localesPending: [tk, ru]` in tracking (time-boxed debt).

---

## 7. Catalog generator (Layer A)

| Phase | Behavior |
|-------|----------|
| **1** | `bo-catalog.json` — English `displayName` (current model) |
| **4+** | Optional `bo-catalog.{locale}.json` from XAF model language aspects |

Reference pages render field labels in the **active site locale**.

---

## 8. CI / pipeline

`Build-UserManual.ps1`:

1. Validate slug parity across `docs/{en,tr,tk,ru}/`
2. `mkdocs build` — all four locales
3. UserManual E2E — start with `en`; add locale matrix when app supports culture in EasyTest host

**UserManualDocs tests:** assert four locale folders exist for each `published` slug (or documented exception in tracking).

---

## 9. Rollout phases

| Phase | Localization deliverable |
|-------|--------------------------|
| **0** | mkdocs-static-i18n; four locale folders; `en` placeholders; switcher live |
| **2** | Pilot guides **published in `en`** |
| **3** | Pipeline validates locale folder structure |
| **4** | tr/tk/ru prose + screenshots for tier 0–4 pilots |
| **5+** | All curriculum tiers in four locales; catalog per culture |

**Success metric:** top 5 guides available in **en, tr, tk, ru** with locale-matched screenshots (or documented interim en fallback).

---

## 10. Agent workflow

```text
@visa2026-user-manual Translate guide person/register to tr, tk, ru from en draft.
· Read content-policy.md + localization.md
· Copy slug structure; translate title and body only
· status: draft per locale
· Do not publish until officer reviewer for that language signs off
```

---

## 11. Changelog

| Date | Change |
|------|--------|
| 2026-08-04 | Initial — four locales en/tr/tk/ru; folder structure; mkdocs-static-i18n |
