# Reference: BO state colors and ListView appearance

Companion to [SKILL.md](./SKILL.md). Canonical colors: [`docs/BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md). State determination overview: same doc § **How BO states are determined**.

---

## State determination matrix

Use when implementing evaluators, `PrimaryStateCode`, or choosing between flag vs computed state.

| ID | Source | Mechanism | Key types / files | Example state codes | Row color source |
|----|--------|-----------|-------------------|---------------------|------------------|
| **A** | Persisted flags | User or sync sets bool on BO | `Visa.IsCancelled`, `IsChanged`, `IsExtended`, `InvitationItem.IsUsed`, `Person.IsArchived`, `ExtensionRequired` | `IsCancelled`, `IsUsed`, `IsArchived` | Flag or evaluator mapping |
| **B** | Date / time | Compare dates to `Today`; `DaysRemaining` | `IExpirationLogic`, `ExpirationLogicHelper`, `ExpirationState`, `SystemSettings` | `Expired`, `ExpiringSoon` → `Expiring`, `Active` | Evaluator (C) |
| **C** | C# evaluator | Static `Evaluate(BO, settings)` → `BoStateResult` | `*StateEvaluator.cs`, `StateSeverity`, `StateEvaluationSettings` | `Cancelled`, `Extended`, `Changed` | `StateCode` + alias → registry |
| **D** | Process history | Latest `ApplicationProgress` by `Date`, `ID` | `ApplicationProgress`, `ApplicationProgressHelper`, `application-state.json`, `application-location.json` | `PROCESS_ISSUED`, `AT_OFFICE`, `1_REVIEW_STARTED` | Catalog `Code` direct |
| **E** | Cross-BO linkage | Join related entities + `ApplicationType.Name` | `ApplicationItem.CurrentVisa`, `IssuingApplicationItem`, `Registration`, `Rejection`/`RejectionItem`, `TravelHistory` | `OnExtension`, person-level `IsRejected` | Evaluator or SQL view |
| **F** | Current vs historical | `PersonCurrentItems` — instance ≠ current | `GetCurrentVisa`, `GetCurrentWorkPermitItem`, `VisaIsEffectiveOn` | `Archived` | Evaluator branch |
| **G** | SQL view | `SqlViewsUpdater` + EF `ToView` entity | `View_VisaExtensionStatus`, `VisaExtensionStatus`, `WorkPermitExtensionStatus` | Extension dashboard states | View column → registry |
| **H** | Configuration | Thresholds / type flags branch logic | `SystemSettings`, `ApplicationType.Show*`, `ApplicationTypeConfigurationSeed` | `ExpiringSoonNotRequired` (subtype) | Inside evaluator; not a color by itself |

### Evaluator vs flag authority (Visa and similar)

Business rules ([`BUSINESS_LOGIC_BASELINE.md`](../../../docs/BUSINESS_LOGIC_BASELINE.md)) may require **linkage evidence**, not flags alone:

| State | Primary evidence (target) | Flag role today |
|-------|---------------------------|-----------------|
| `Cancelled` | Cancel application via `ApplicationItem` / `Registration` ([BR-043](BUSINESS_LOGIC_BASELINE.md)) | `IsCancelled` — UI/storage; evaluator also checks flag |
| `OnExtension` | Extension `ApplicationItem.CurrentVisa`, no `IssuingApplicationItem` ([BR-044](BUSINESS_LOGIC_BASELINE.md)) | Computed in evaluator/SQL, not a single BO column |
| Person rejected | `RejectionItem` for person ([BR-064](BUSINESS_LOGIC_BASELINE.md)) | Not `PROCESS_REJECTED` alone |
| Extension complete | `IssuingApplicationItem` on extension item ([BR-035](BUSINESS_LOGIC_BASELINE.md)) | Drives `IsExtended` / prolonged semantics |

When implementing **row color**, follow **evaluator/SQL outcome** for display even if a flag exists for forms.

### Multi-dimensional example (Visa)

| Dimension | Source | Example codes | Display |
|-----------|--------|---------------|---------|
| Validity | B + C | `Expiring`, `Expired` | Row winner if highest severity |
| Document | A | `IsExtended`, `IsChanged` | Column accent |
| Process | D + E | `OnExtension`, `1_REVIEW_STARTED`, `AT_THE_MINISTERY_1` | Column accent or second column |
| Scope | F | `Archived` | Gray when not current |

### Alias map (evaluator → registry)

Maintain in `BoStateAppearanceColors` or BO `PrimaryStateCode` getter:

| Evaluator `StateCode` | Registry code |
|----------------------|---------------|
| `ExpiringSoon` | `Expiring` |
| `ExpiringSoonNotRequired` | `Expiring` (same family; optional badge text differs) |
| `Cancelled` | `IsCancelled` |
| `Changed` | `IsChanged` |
| `Extended` | `IsExtended` |
| `Active` | *(no row tint)* |
| `Archived` | `IsArchived` |

---

## File map

| Path | Purpose |
|------|---------|
| `docs/BO_STATE_COLORS.md` | State code → family, tone, XAF `BackColor`, CSS hex |
| `docs/BO_STATE_TEMPORAL_TYPES.md` | **`DaysRemaining`** vs **`DaysElapsed`** per BO; anchor dates |
| `docs/BO_STATE_TRACKING.md` | State definitions per BO |
| `docs/STATE_SPECIFICATIONS.md` | Dashboard state specs |
| `Visa2026.Module/Services/StateEvaluation/` | Evaluators, `BoStateResult`, `StateSeverity` |
| `Visa2026.Module/Services/StateEvaluation/Evaluators/*.cs` | Per-BO evaluation |
| `Visa2026.Module/BusinessObjects/ApplicationProgress.cs` | Workflow history rows |
| `Visa2026.Module/BusinessObjects/ApplicationProgressHelper.cs` | Latest progress resolution |
| `Visa2026.Module/DatabaseUpdate/LookupCatalogs/application-state.json` | `ApplicationState` seed |
| `Visa2026.Module/DatabaseUpdate/LookupCatalogs/application-location.json` | `ApplicationLocation` seed |
| `Visa2026.Module/Appearance/SoftDeleteAppearanceRegistration.cs` | Pattern for bulk `[Appearance]` registration |
| `Visa2026.Module/BusinessObjects/Visa.cs` | Current `StateSeverityLevel` + 3-bucket appearance |
| `Visa2026.Blazor.Server/Controllers/SoftDeleteGridRowAppearanceController.cs` | Blazor grid row CSS fallback |
| `Visa2026.Blazor.Server/wwwroot/css/site.css` | `.bo-state-inbox__*`, `.visa-soft-deleted-row` |

**Planned (not yet in repo — create when implementing row-color rollout):**

| Path | Purpose |
|------|---------|
| `Visa2026.Module/Appearance/BoStateAppearanceColors.cs` | Registry: `TryGet(stateCode, out appearance)` |
| `Visa2026.Module/Appearance/BoStateRowAppearanceRegistration.cs` | `CustomizeTypesInfo` registrar |
| `Visa2026.Module/BusinessObjects/IBoListRowState.cs` | `PrimaryStateCode`, optional `StateDisplayPriority` |
| `Visa2026.Blazor.Server/Controllers/BoStateGridRowAppearanceController.cs` | Nested ListView row classes |

---

## State code namespaces

Use stable string codes consistently across evaluators, appearance, and docs.

| Source | Code examples | Notes |
|--------|---------------|-------|
| Document flags | `IsCancelled`, `IsChanged`, `IsExtended`, `IsArchived`, `IsRegistered`, `IsUsed` | Property names as codes in registry |
| Evaluator results | `Active`, `Expired`, `ExpiringSoon`, `Cancelled`, `Changed`, `Extended`, `Archived` | Map to registry (`ExpiringSoon` → `Expiring`) |
| Conceptual (UI) | `OnProcess`, `OnExtension`, `AtOffice`, `ProcessComplete` | Used when no finer `ApplicationState` code |
| `ApplicationState.Code` | `PROCESS_ISSUED`, `1_REVIEW_STARTED`, … | Exact catalog code |
| `ApplicationLocation.Code` | `AT_OFFICE`, `AT_MIGRATION_SERVICE`, … | Exact catalog code |

Evaluator `StateCode` and registry code may differ — document alias in `BO_STATE_COLORS.md` § Semantic aliases or add explicit mapping in `BoStateAppearanceColors`.

---

## `BoStateAppearanceColors` (recommended shape)

```csharp
namespace Visa2026.Module.Appearance;

public readonly record struct BoStateAppearance(
    string StateCode,
    string BackColor,
    string FontColor,
    string CssBackgroundHex,
    string CssTextHex,
    int DisplayPriority); // maps to severity order for row conflicts

public static class BoStateAppearanceColors
{
    private static readonly IReadOnlyDictionary<string, BoStateAppearance> Registry =
        new Dictionary<string, BoStateAppearance>(StringComparer.OrdinalIgnoreCase)
        {
            ["IsCancelled"] = new("IsCancelled", "LightCoral", "Red", "#f8d7da", "#842029", 300),
            ["Expiring"] = new("Expiring", "LightSalmon", "DarkOrange", "#ffd8a8", "#7c2d12", 200),
            // … every code from docs/BO_STATE_COLORS.md
        };

    public static bool TryGet(string stateCode, out BoStateAppearance appearance) =>
        Registry.TryGetValue(stateCode, out appearance);

    public static IReadOnlyCollection<BoStateAppearance> All => Registry.Values;
}
```

Populate from [`docs/BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md) master tables. **`DisplayPriority`:** Critical 300+, Warning 200+, Info 100+, Healthy 50, Archived 10.

---

## `IBoListRowState` (recommended)

```csharp
namespace Visa2026.Module.BusinessObjects;

/// <summary>BO supplies the single state code used for ListView row background.</summary>
public interface IBoListRowState
{
    [Browsable(false)]
    [NotMapped]
    string PrimaryStateCode { get; }

    /// <summary>When multiple IBoListRowState dimensions exist on one object, higher wins at same priority tier.</summary>
    [Browsable(false)]
    [NotMapped]
    int StateDisplayPriority { get; }
}
```

**Example — Visa (sketch):** `PrimaryStateCode` from `VisaStateEvaluator.Evaluate(...).StateCode` with alias map (`ExpiringSoon` → `Expiring`, `Cancelled` → `IsCancelled`).

**Example — Application (sketch):** `PrimaryStateCode` from latest `ApplicationProgress.State.Code`, fallback `Location.Code`, per `BO_STATE_COLORS.md` § Combined application display.

---

## Row appearance registration (pattern)

Mirror `SoftDeleteAppearanceRegistration`:

```csharp
internal static class BoStateRowAppearanceRegistration
{
    public static void Register(ITypesInfo typesInfo)
    {
        foreach (var appearance in BoStateAppearanceColors.All)
        {
            foreach (ITypeInfo typeInfo in typesInfo.PersistentTypes)
            {
                if (typeInfo.Type == null || !typeof(IBoListRowState).IsAssignableFrom(typeInfo.Type))
                    continue;

                typeInfo.AddAttribute(new AppearanceAttribute($"BoStateRow_{appearance.StateCode}")
                {
                    AppearanceItemType = "ViewItem",
                    TargetItems = "*",
                    Criteria = $"PrimaryStateCode = '{appearance.StateCode}'",
                    Context = "ListView",
                    BackColor = appearance.BackColor,
                    Priority = appearance.DisplayPriority,
                });
            }
        }
    }
}
```

Register in `Module.CustomizeTypesInfo` **after** `SoftDeleteAppearanceRegistration` (delete rule keeps priority 500).

**Boolean flags without `IBoListRowState`:** per-BO `[Appearance]` on `IsCancelled`, etc., using registry `BackColor`/`FontColor` for that code only (`TargetItems = "IsCancelled"`, `Criteria = "IsCancelled = true"`).

---

## Existing 3-bucket appearance (to migrate)

```csharp
// Visa.cs — replace when BoStateRowAppearanceRegistration covers Visa PrimaryStateCode
[Appearance("VisaStateInfo", Priority = 100, ...,
    Criteria = "IsDeleted = false And StateSeverityLevel = 1", BackColor = "LightSkyBlue")]
[Appearance("VisaStateWarning", Priority = 200, ...,
    Criteria = "... StateSeverityLevel = 2", BackColor = "LightSalmon")]
[Appearance("VisaStateCritical", Priority = 300, ...,
    Criteria = "... StateSeverityLevel >= 3", BackColor = "LightCoral")]
```

Same pattern on `WorkPermitItem.cs`, partial on `Passport.cs` (no Info bucket).

**Migration steps per BO:**

1. Add `PrimaryStateCode` (+ alias map from evaluator codes to registry codes).
2. Implement `IBoListRowState`.
3. Remove three `[Appearance]` attributes.
4. Rely on registrar **or** BO-specific rules until registrar lands.
5. Verify ListView manually; append [learnings.md](./learnings.md) if nested.

---

## Blazor nested ListView fallback

`SoftDeleteGridRowAppearanceController` sets `e.CssClass = "visa-soft-deleted-row"` because model `[Appearance] BackColor` is unreliable on nested DxGrid ListViews.

**For state colors:**

1. Add CSS classes in `site.css`: `.visa-bo-state--{state-code-kebab}` { background-color: … } from registry hex.
2. Controller targets `IBoListRowState` (or specific BO types).
3. In `GridCustomizeElement`, read `PrimaryStateCode`, append class.

Kebab-case: `PROCESS_ISSUED` → `visa-bo-state--process-issued`, `1_REVIEW_STARTED` → `visa-bo-state--1-review-started`.

---

## Application progress criteria (XAF)

Latest state code filter (from `ApplicationProgressHelper`):

```text
ProgressHistory[Date = ^.ProgressHistory.Max(Date)].Single(State.Code) = 'PROCESS_STARTED'
```

Use in `[Appearance]` on `Application` or filtered views. For list row color, prefer computed `PrimaryStateCode` on `Application` instead of long criteria strings.

---

## Dashboard / inbox CSS

State notification prototype uses severity buckets in `site.css` (`.bo-state-inbox__card--critical`, etc.). When aligning with per-state colors:

- Either map inbox cards to **family** (Red/Amber/Blue…) for scanability, or
- Add per-code classes using registry `CssBackgroundHex`.

Do not diverge from `BO_STATE_COLORS.md` hex without updating the doc.

---

## Migration checklist (solution-wide)

```
Phase 1 — Registry in code
- [ ] BoStateAppearanceColors populated from BO_STATE_COLORS.md
- [ ] Unit test: every registry key has unique BackColor + CssBackgroundHex

Phase 2 — Evaluator alias map
- [ ] ExpiringSoon → Expiring, Cancelled → IsCancelled, etc.

Phase 3 — BOs
- [ ] Visa, WorkPermitItem, Passport, Application, InvitationItem, …

Phase 4 — Registrar + Module.CustomizeTypesInfo

Phase 5 — Blazor BoStateGridRowAppearanceController for nested views

Phase 6 — Remove obsolete severity-bucket [Appearance] attributes
```

---

## Troubleshooting

| Symptom | Likely cause | Action |
|---------|--------------|--------|
| Row never tints | `[NotMapped]` property not evaluated in criteria | Use persistent computed field or controller |
| Nested tab grid plain | Blazor appearance gap | CSS controller + class |
| Wrong color with multiple states | Priority / severity | Fix `PrimaryStateCode` resolution; column accents for secondary |
| Dashboard count ≠ list filter | Evaluator ≠ SQL view | STATE_SPECIFICATIONS parity — not a color issue |
| New state gray/default | Missing registry entry | Add to BO_STATE_COLORS.md + BoStateAppearanceColors |
