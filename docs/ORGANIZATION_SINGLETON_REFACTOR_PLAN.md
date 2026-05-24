# Organization model — singleton refactor plan

Refactoring plan to replace multi-row **organization** business objects with **singleton configuration** types (one row per database deployment). Each customer runs a **single company**; org identity belongs in **settings**, not as foreign keys on `Application`, `Person`, and related BOs.

**In scope:** `Company`, `CompanyHead`, `Representative`, `LocalEmployee`, and their use from domain BOs, reports, PDF mapping, security/navigation, DataImporter, and E2E.

**Out of scope (this plan):** multi-tenant SaaS (tenant id on rows), redesign of `Person` / expat HR beyond removing `Person.Company`, wholesale rewrite of predefined XtraReport `.cs` layouts (prefer placeholder aliases first).

**Status:** Phase 4 complete — Phase 5 (schema drop) optional / later.

**Related:** [`docs/DEPRECATED.md`](DEPRECATED.md), [`Visa2026.Module/BusinessObjects/SystemSettings.cs`](../Visa2026.Module/BusinessObjects/SystemSettings.cs), [`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`](WORD_REPORT_PLACEHOLDER_REFERENCE.md), [`Visa2026.Module/Resources/AppNumberFormat.md`](../Visa2026.Module/Resources/AppNumberFormat.md).

---

## 1. Problem (current model)

Today the app models one organization as **many rows** and wires them into transactional BOs:

| Legacy type | Pattern | Used for |
|-------------|---------|----------|
| **Company** | Multi-row + `IsDefault` | `Application.Company`, `Person.Company`, app numbering, letterhead `Code`, collections (heads, reps, local staff, contracts, images) |
| **CompanyHead** | `SingleActiveBaseObject<Company, CompanyHead>` + LocalEmployee **or** Person | `Application.CompanyHead`; signatory on Word/XtraReports (`Application_CompanyHead_*`, `CompanyHead_*`) |
| **Representative** | Same as CompanyHead | `Application.Representative`; rep placeholders on `ApplicationItem` |
| **LocalEmployee** | List scoped to `Company` | Feeds CompanyHead / Representative pickers only |

```142:171:Visa2026.Module/BusinessObjects/Application.cs
        public virtual Company Company { get; set; }
        // ...
        public virtual CompanyHead CompanyHead { get; set; }
        // ...
        public virtual Representative Representative { get; set; }
```

**Pain for users:** pick company → pick signatory/representative from lists tied to company; change “active” head/rep elsewhere. **Pain for implementers:** `DataSourceCriteria`, `SingleActiveBaseObject` sibling logic, `[NotMapped]` `FullName` caches for report disposal, and report paths like `Application.Company.Name` / `Application.CompanyHead.FullName`.

**Product direction:** one org per database → **singleton profile rows** edited under Organization/Settings, **no FKs** on `Application` / `Person`, reports read singletons (directly or via stable `[NotMapped]` aliases).

---

## 2. Goals

1. Introduce **new singleton BOs** (new type names — do not rename legacy tables in place).
2. **No relationships** between new singletons (no FK Company → Representative).
3. **Flat fields** on signatory/representative singletons (no link to `LocalEmployee` / `Person` for report identity).
4. **Remove** `Company`, `CompanyHead`, `Representative`, `LocalEmployee` from **new** domain usage; deprecate legacy types per [`docs/DEPRECATED.md`](DEPRECATED.md).
5. Preserve **report and template compatibility** during migration (`Application_CompanyHead_FullName`, `Company_Name`, etc. via `[NotMapped]` delegates).
6. Move **application numbering** off `Company` onto existing **`SystemSettings`** (or document a dedicated singleton if settings grow too large).
7. Keep legacy **tables/columns** until importers and production DBs are migrated (same pattern as `ProjectContract.Company`).

---

## 2.1 Decisions

| # | Topic | Decision | Status |
|---|--------|----------|--------|
| 1 | New type names | **`CompanyProfile`**, **`AuthorizedSignatory`**, **`AuthorizedRepresentative`** only (no fourth org singleton) | Locked |
| 2 | Signatory position | **`PositionTitleTm`** (or `PositionTitle`) — plain **`string`**, not a `Position` lookup FK | Locked |
| 3 | Local employee / `LocalContact` | **No `LocalContact` singleton** — retire `LocalEmployee`; signatory and representative each store their own flat name/position/contact fields | Locked (see §2.2) |
| 4 | App numbering | Fields on **`SystemSettings`**: `AppNumberPrefix`, `AppNumberFormat`, `ApplicationNumberSeed`, `ApplicationNumberPadding` | Locked |
| 5 | Letterhead assets | Keep **`CompanyProfile.Code`** for `AppBaseReport` background resolution | Locked |
| 6 | Legacy child BOs | **Phase 2+:** `CompanyImage`, `CompanyDocument`, `*Head*`, `*Representative*` — static resources or file fields on singletons | Planned |
| 7 | `Person.Company` | Remove FK + default-on-create; fix `AddressOfResidence` / other `DataSourceCriteria` that reference `Person.Company` | Planned |
| 8 | `ProjectContract.Company` | Already legacy/hidden — no new usage | — |
| 9 | Placeholder stability | Keep existing **string keys** in Word/user reports; implementation reads singletons | Locked |

### 2.2 What “LocalContact” meant (and why we skip it)

**Today:** `LocalEmployee` is a **list** of local nationals under `Company`. Almost nothing on `Application` points at that list directly. Instead, **CompanyHead** (signatory) and **Representative** say: “pick someone” — either a row from `LocalEmployee` **or** an expat `Person`. That forces two pickers and a hidden roster users rarely open (navigation to `LocalEmployee` is already denied for normal roles).

**The old plan question** was whether, after refactor, you still need a **third** settings screen:

| Option | Meaning | Admin screens |
|--------|---------|----------------|
| **Separate `LocalContact` singleton** | A generic “our local staff / HR contact” record **in addition to** signatory and representative | Company + Signatory + Representative + **Local contact** |
| **Merged (chosen)** | No roster, no third singleton. Letters use **AuthorizedSignatory**; forms use **AuthorizedRepresentative**. Each has its own typed `FullName`, position **string**, passport/phone fields | Company + Signatory + Representative only |

**Choose merged** when signatory and representative are the only local identities reports need (typical for one company: director signs letters, one person is “authorized rep” on applications — they may be the same person or two different people, but you do not need a separate employee directory).

**You would only add `LocalContact` later** if a new report or workflow needs a third named role (e.g. “HR contact” on forms) that is neither signatory nor representative.

**Migration:** seed updater copies active `CompanyHead` → `AuthorizedSignatory`, active `Representative` → `AuthorizedRepresentative`. Legacy `LocalEmployee` rows are not copied to a new BO; optional one-time copy only if head/rep had been linked to a local employee row (flatten name/position from that link).

### 2.3 Pre-implementation decisions (locked 2026-05-24)

| Topic | Decision |
|-------|----------|
| Report data source | **Live singletons** — not per-application FK snapshots |
| Passport fields | On **both** `AuthorizedSignatory` and `AuthorizedRepresentative` (`PassportNumber`, `PassportAuthority`, `PassportIssueDate`, computed `PassportLine`) |
| Phone | **`AuthorizedRepresentative.Phone` only** |
| Representative title | **`PositionTitleTm`** string on representative (may be empty) |
| Names | Single **`FullName`** string per singleton |
| Letterhead | **`CompanyProfile.Code`** + static `background_{Code}.jpg` only in Phase 1 |
| Delivery | **Phase 1 only first**, then Phase 2+ in follow-ups |
| Legacy schema | **Keep tables** until production sign-off (Phase 5) |
| Multi-company DB | Seed from `IsDefault` company, else first row |
| DataImporter / E2E | **Phase 4** follow-up (not Phase 1) |

---

## 3. Target architecture

### 3.1 New singleton types

Follow **`SystemSettings`** pattern: `TryGetInstance(IObjectSpace)`, `GetOrCreateInstance(IObjectSpace)`, `OnCreated` defaults, optional `ModuleUpdater` seed.

| New BO | Navigation (suggested) | Properties (minimum) |
|--------|------------------------|----------------------|
| **CompanyProfile** | Organization → Company | `Name`, `Code`, `Address`, `PhoneNumber`, `Email`, `TaxInformation` |
| **AuthorizedSignatory** | Organization → Signatory | `FullName`, **`PositionTitleTm`** (`string`), passport fields needed by reports (`PassportNumber`, `Authority`, `IssueDate`, `PassportLine` computed) |
| **AuthorizedRepresentative** | Organization → Representative | `FullName`, **`PositionTitleTm`** (`string`, if needed on forms), phone, passport line fields (mirror current `ApplicationItem` rep placeholders) |

**Rules:**

- No `virtual Company X` on new types.
- No `SingleActiveBaseObject` on new types.
- One row enforced by query + updater (delete extraneous rows on upgrade if any).

### 3.2 SystemSettings extension (application numbers)

Move from legacy `Company`:

| Property | Notes |
|----------|--------|
| `AppNumberPrefix` | Was on Company |
| `AppNumberFormat` | Tokens documented in `AppNumberFormat.md` |
| `ApplicationNumberSeed` | Continue-from-external-system seed |
| `ApplicationNumberPadding` | Default 4 |

Update `Application` save logic to read **`SystemSettings.GetOrCreateInstance`** instead of `Company`.

### 3.3 Application / ApplicationItem surface

**Remove persisted FKs** (phased):

- `Application.Company`
- `Application.CompanyHead`
- `Application.Representative`

**Keep or add `[NotMapped]` report aliases** (delegate to singletons), e.g.:

| Existing alias (keep name) | Reads from |
|----------------------------|------------|
| `Company_Code`, `Application` company name/address helpers | `CompanyProfile` |
| `Application_CompanyHead_*`, `CompanyHead_*` on `ApplicationItem` | `AuthorizedSignatory` |
| `Representative_*` on `ApplicationItem` | `AuthorizedRepresentative` |

Implementation sketch:

```csharp
[NotMapped]
public string Application_CompanyHead_FullName =>
    AuthorizedSignatory.TryGetInstance(ObjectSpace)?.FullName ?? string.Empty;
```

Remove `OnCreated` / `Company` setter logic that copies `CurrentRepresentative` from company.

### 3.4 Person and other BOs

| Area | Action |
|------|--------|
| **Person** | Remove `Company`; remove default `IsDefault` company query in `OnCreated` |
| **AddressOfResidence** | Remove `DataSourceCriteria` tied to `Person.Company` |
| **Lodging** | Review any `Company` reference |
| **LookupCatalogEntitySync** | Stop matching import rows by `Company` name where replaced by single-tenant assumption |
| **PdfFormMapping** | Point paths at singleton accessors or `Application` aliases |

### 3.5 Reports and services

| Layer | Action |
|-------|--------|
| **Word report defs** (`Services/WordReports/*`) | Prefer `CompanyProfile` / signatory / rep singletons or `Application` `[NotMapped]` aliases |
| **UserReportMergeDataHelper** | `CompanyName` from `CompanyProfile` |
| **AppBaseReport** | Letterhead: `CompanyProfile.Code`; drop CompanyHead graph preload where aliases no longer navigate EF |
| **Predefined XtraReports** | Phase B: expression bindings can keep `[Application_CompanyHead_*]` if aliases remain on `Application` / `ApplicationItem` |
| **Docs** | Update `WORD_REPORT_PLACEHOLDER_REFERENCE.md`, `REPORT_GENERATION_GUIDE.md`, form `*_map.md` notes |

### 3.6 UI / security

- New navigation group **Organization** (or under **Settings**): singleton detail views (disable list create/delete of second row).
- Legacy nav items: hide + deny Create; Read optional for admin audit until removal.
- Update `DatabaseUpdate/Updater.cs` navigation permissions (today some `LocalEmployee` paths are denied — reconcile with new types).

---

## 4. Legacy deprecation

Register in **[`docs/DEPRECATED.md`](DEPRECATED.md)** when implementation starts:

| Name | Status | Replacement | Schema |
|------|--------|-------------|--------|
| **Company** | Deprecated | `CompanyProfile` + `SystemSettings` (numbering) | Keep table until column/FK drop phase |
| **CompanyHead** | Deprecated | `AuthorizedSignatory` | Keep table |
| **Representative** | Deprecated | `AuthorizedRepresentative` | Keep table |
| **LocalEmployee** | Deprecated | Fields folded into `AuthorizedSignatory` / `AuthorizedRepresentative` (no replacement BO) | Keep table |
| **CompanyImage**, **CompanyDocument**, **CompanyHeadImage**, etc. | Retained → Removed | Singleton file fields or `Resources/` | Per-phase |

Mark C# `[Obsolete("Use CompanyProfile / …")]` on legacy types when callers are migrated.

---

## 5. Data migration (ModuleUpdater)

**Updater name (suggested):** `OrganizationSingletonSeedUpdater` (runs after DB schema includes new tables).

| Step | Logic |
|------|--------|
| 1 | If `CompanyProfile` empty: copy from `Company` where `IsDefault == true`, else first company |
| 2 | If `AuthorizedSignatory` empty: copy from `company.CurrentAuthorizedSignatory` or active `CompanyHead` (`IsActive`, not soft-deleted) |
| 3 | If `AuthorizedRepresentative` empty: copy from `company.CurrentRepresentative` or active `Representative` |
| 4 | If `SystemSettings` exists and numbering fields empty: copy numbering fields from default `Company` |
| 5 | When seeding signatory/rep: if legacy row linked `LocalEmployee`, flatten `FullName` / position text from that link into the new string fields |
| 6 | Do **not** delete legacy rows in Phase 1 |

**Idempotency:** safe to re-run; only fill empty singletons unless `FORCE_*` flag is introduced (follow `FORCE_XAF_DB_UPDATE` pattern in [`docs/ENVIRONMENTS.md`](ENVIRONMENTS.md) if needed).

---

## 6. Implementation phases

### Phase 1 — Foundation (low risk)

- [x] Add `CompanyProfile`, `AuthorizedSignatory`, `AuthorizedRepresentative` BOs + EF configuration in `Visa2026DbContext`
- [x] Add `TryGetInstance` / `GetOrCreateInstance` on each
- [x] Extend `SystemSettings` with app numbering fields + defaults
- [x] Add `OrganizationSingletonSeedUpdater` (registered in `Module.cs`)
- [ ] XAF model entries (Module `Model.DesignedDiffs.xafml` + Blazor `Model.xafml` as needed) — optional layout tweaks via Model Editor
- [x] Navigation: **Organization** group via `[NavigationItem("Organization")]` on new types
- [x] **DEPRECATED.md** stubs for legacy org BOs

**Exit criteria:** Admin can edit singletons; seed populates from legacy data on upgrade. **Requires one app/DB update** after deploy to create tables and run seed.

### Phase 2 — Read path for reports (no FK removal yet)

- [x] Add `[NotMapped]` aliases on `Application` / `ApplicationItem` reading singletons (`OrganizationReportHelper`)
- [x] Switch `Services/WordReports/*` and `UserReportMergeDataHelper` to aliases
- [x] Adjust `AppBaseReport` — removed `EagerLoadSignatoryNavigations`; letterhead uses `Company_Code`
- [x] Update `PdfFormMappingUpdater` seed + `OrganizationPdfFormMappingUpdater` for existing DBs
- [x] Point `Application.OnSaving` numbering to `SystemSettings`
- [x] XtraReport bindings/RTF: `[Company.Name]` → `[Application_Company_Name]`; signatory bindings on `AppBaseReport`

**Exit criteria:** Reports and PDFs read live singleton data. Legacy `Application.Company` / `CompanyHead` / `Representative` FKs still on BO (Phase 3).

### Phase 3 — Remove domain FKs

- [x] Remove `Application.Company`, `CompanyHead`, `Representative` (+ UI model layout)
- [x] Remove `Person.Company` and related criteria
- [x] Hide legacy list views; `[Obsolete]` on legacy BO types
- [x] Update security roles in `Updater.cs`

**Exit criteria:** New applications/persons do not reference legacy org FKs.

### Phase 4 — Tooling and tests

- [x] **DataImporter:** singleton upsert (`CompanyProfile`, signatory, rep); Excel mappings; `OrganizationSingletonImporter`; update `IMPORTING.md`
- [x] **E2E:** `OrganizationSettingsTests` replaces `CompanyTests`
- [x] **Web API** (`WebApiServiceExtensions.cs`): `CompanyProfile`, `AuthorizedSignatory`, `AuthorizedRepresentative`, `SystemSettings`
- [x] **Lookup export:** `tenant/README.md` documents `project-contract.json` `Company` column vs `CompanyProfile`

**Exit criteria:** CI green; import scenarios documented.

### Phase 5 — Schema cleanup (optional, later)

- [ ] Drop FK columns: `Applications.Company`, `CompanyHead`, `Representative`; `People.Company`; etc.
- [ ] Drop or archive legacy tables after backup policy confirmed
- [ ] Move **DEPRECATED.md** rows to **Removed**

**Exit criteria:** EF model has no legacy org entities (or only retained for audit import).

---

## 7. File / area checklist

| Area | Files / folders |
|------|-----------------|
| New BOs | `Visa2026.Module/BusinessObjects/CompanyProfile.cs`, `AuthorizedSignatory.cs`, `AuthorizedRepresentative.cs`, … |
| DbContext | `Visa2026.Module/BusinessObjects/Visa2026DbContext.cs` |
| Updaters | `Visa2026.Module/DatabaseUpdate/OrganizationSingletonSeedUpdater.cs`, `Module.cs` registration |
| Domain | `Application.cs`, `ApplicationItem.cs`, `Person.cs`, `AddressOfResidence.cs` |
| Legacy (deprecate) | `Company.cs`, `CompanyHead.cs`, `Representative.cs`, `LocalEmployee.cs`, child image/document types |
| Reports | `Visa2026.Module/Reports/`, `Services/WordReports/`, `Services/UserReports/` |
| Docs | `DEPRECATED.md`, `WORD_REPORT_PLACEHOLDER_REFERENCE.md`, `BUSINESS_LOGIC_BASELINE.md` |
| Tests | `Visa2026.E2E.Tests/CompanyTests.cs`, `E2ETestBase.cs` |
| Import | `Visa2026.DataImporter/CompanyImporter.cs`, `CompanyHeadImporter.cs`, `RepresentativeImporter.cs`, `LocalEmployeeImporter.cs` |

Use ripgrep before each phase:

```powershell
rg "\bCompany\b|\bCompanyHead\b|\bRepresentative\b|\bLocalEmployee\b" Visa2026.Module Visa2026.DataImporter Visa2026.E2E.Tests --glob "*.cs"
```

---

## 8. Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Report output changes (signatory name, letterhead) | Phase 2 comparison tests; seed updater copies active legacy rows |
| Old applications stored FK to old `CompanyHead` | Aliases ignore stored FKs after Phase 3; optional one-time “refresh” not needed if always read singleton |
| App number sequence reset | Copy seed/prefix/format exactly in updater; document in deploy notes |
| XtraReport runtime expressions still use old binding paths | Keep `[NotMapped]` names on `Application` / `ApplicationItem` |
| Import scripts create multiple companies | Importer writes one `CompanyProfile`; legacy company rows become read-only |
| Passport data on signatory today via linked Person | Flatten passport fields onto `AuthorizedSignatory` during seed |

---

## 9. Verification

**Build / test (repo root):**

```powershell
dotnet build Visa2026.slnx -c Debug
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c Debug
```

**Manual smoke:**

1. Open Organization → edit Company / Signatory / Representative singletons.
2. Create Application — no company/rep pickers; numbering still works.
3. Generate one Word ministry letter + one user template + one PDF with company fields.
4. Compare signatory block to pre-migration sample document.

---

## 10. Open questions

1. **When to drop legacy tables:** default = after production sign-off (Phase 5); confirm at Phase 3 gate.
2. **Company collections** (`ProjectContracts`, `Employees` on Company): confirm no admin workflow still lists “all employees under company” before removing legacy nav (Phase 3+).

---

## 11. Changelog

| Date | Author | Note |
|------|--------|------|
| 2026-05-24 | — | Initial plan (Company, CompanyHead, Representative, LocalEmployee → singletons). |
| 2026-05-24 | — | Locked: signatory `PositionTitleTm` as string; no `LocalContact` singleton (§2.2). |
| 2026-05-24 | — | Locked pre-implementation defaults (§2.3); Phase 1 implementation started. |
| 2026-05-24 | — | Phase 2: report/PDF read path + `SystemSettings` app numbering. |
| 2026-05-24 | — | Phase 3: removed `Application`/`Person` org FKs; legacy BOs hidden + `[Obsolete]`. |
| 2026-05-24 | — | Phase 4: DataImporter singleton upsert, Web API, E2E, lookup docs. |
