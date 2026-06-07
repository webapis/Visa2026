# UI test hooks — reference

Mechanism matrix and copy-paste patterns. Canonical workflow: [SKILL.md](./SKILL.md).

---

## UI element categories (mechanism families)

Same table as [SKILL.md § UI element categories](./SKILL.md#ui-element-categories-mechanism-families). When adding hooks, **identify the family first** — then **match** prior art before writing code.

---

## Classify and match (before code)

Run this **before** any edit to controllers or `Model.xafml`. Canonical workflow: [SKILL.md § Before writing code](./SKILL.md#before-writing-code-classify--match--reuse--discover).

### Classify checklist

| Question | Output |
|----------|--------|
| What UI control is it? | field / tab / action / logon / nav / grid / MDI |
| Family **A–F**? | See table below + decision tree |
| Stable id? | member name, `LayoutGroup` Id, Action Id, nav item Id |
| In default hook scope? | [SKILL.md § BO member scope](./SKILL.md#bo-member-scope--what-to-hook) |

### Match checklist (search order)

| # | Source | If found… |
|---|--------|-----------|
| 1 | [registry.md](./registry.md) | Same family + **verified** → **Reuse** mechanism column |
| 2 | [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) | Copy DevTools snippet + DOM target |
| 3 | [reference.md](./reference.md) § Element matrix | Copy code pattern section |
| 4 | [learnings.md](./learnings.md) | Avoid listed pitfalls; apply listed fixes |
| 5 | `Visa2026.Blazor.Server/Controllers/*E2e*` | Closest controller file to extend |

| Match result | Next step |
|--------------|-----------|
| Verified hook, same family | **Reuse** — extend map/list only ([SKILL.md § 4a Reuse](./SKILL.md#4a-reuse-path-default--minutes-not-hours)) |
| Implemented but not verified | Reuse mechanism; still run full DevTools before **verified** |
| No hook, family **A–E** with matrix row | **Reuse** pattern from matrix; first instance may need one verify pass |
| Family **F** or no matrix row | **Discover** loop ([SKILL.md § 4b Discover](./SKILL.md#4b-discover-path-new-family-or-first-of-its-kind)) |

### Verified reuse anchors (copy these, do not re-spike)

| Family | Verified anchor in repo |
|--------|-------------------------|
| **A** | `PersonDetailViewE2eSelectorsController`, `PersonE2eMemberHooks`, `E2ePropertySelectorApplicator` |
| **B** | `PersonDetailViewE2eTabSelectorsController`, `BlazorLayoutManager.ItemCreated` |
| **C** | `LogonViewE2eSelectorsController` (Logon action) — pattern for toolbar `CustomizeControl` |
| **D** | `LogonViewE2eSelectorsController`, `CreateLogonWindowControllers` |
| **E** | `NavigationE2eSelectorsController`, `NavigationE2eHooks`, `NavigationE2eSelectorSupport` |
| **F** | None — **Discover** only; spike then append learnings |

| Family | When user says… | Files you touch (typical) |
|--------|-----------------|---------------------------|
| **A** Scalar | “hook Person.FirstName” | `{Bo}E2eMemberHooks`, `{Bo}DetailViewE2eSelectorsController`, applicator |
| **B** Tab | “hook Passports tab” | `{Bo}DetailViewE2eTabSelectorsController`, optional `Model.xafml` |
| **C** Action | “hook Save button” | ViewController + `CustomizeControl` |
| **D** Logon | “hook login” | `LogonViewE2eSelectorsController`, `BlazorApplication.cs` |
| **E** Sidebar nav | “hook Application nav items” | **`NavigationE2eHooks` map only** (+ shared controller/support) |
| **F** Grid / MDI | “hook grid row” | Backlog — **Discover** loop |

---

## Element type → mechanism

| UI element | Stable id source | Primary mechanism | Fallback | Verified example |
|------------|------------------|-------------------|----------|------------------|
| **String BO field** | BO member name | `ViewController` + `E2ePropertySelectorApplicator` (`InputId`, `data-testid`, `CssClass`) | `[ModelDefault("CustomCSSClassName")]` on property | `Person.FirstName` → `#person-first-name` |
| **Date / lookup / bool / image BO field** | BO member name | Same controller pattern; applicator + reflection fallback on editor `ComponentModel` | `ModelDefault` on property | `Person.DateOfBirth` → `person-date-of-birth` |
| **Collection on detail** (`IList<T>`) | **LayoutGroup** Id, not property name | Tab controller — `HeaderCssClass` + `data-testid` on tab header | `Model.xafml` on layout group | `Passports` → `person-employee-tab-passports` |
| **Logon UserName / Password** | `UserName`, `Password` | `LogonViewE2eSelectorsController` + **`CreateLogonWindowControllers`** | `Model.xafml` layout classes | `#login-user-name`, `#login-password` |
| **Logon button** | Action Id `Logon` | `Action.CustomizeControl` → `DxToolbarItemSimpleActionControl` | `Model.xafml` `ActionDesign` | `[data-testid="login-submit"]` |
| **Layout tab page** | `LayoutGroup` Id under `TabbedGroup` | `BlazorLayoutManager.ItemCreated` → `DxFormLayoutTabPageModel.HeaderCssClass` + `SetAttribute` | `Model.xafml` `CustomCSSClassName` on layout group | Passports → `person-employee-tab-passports` |
| **Layout tab strip** | `TabbedGroup` Id (e.g. `Tabs`) | `ItemCreated` → `DxFormLayoutTabPagesModel` | `Model.xafml` on `TabbedGroup` | `person-employee-tabs` |
| **Standard action** | Action Id | `CustomizeControl` on toolbar item + optional `Model.xafml` | — | Log In (see logon) |
| **Custom property editor** | Member + editor alias | Dedicated `ViewController` + component model | — | TBD (e.g. ApplicationType quick code) |
| **ListView / grid cell** | ListView Id + column | TBD — `DxGridListEditor` / column model | — | Not implemented |
| **Sidebar nav item** | Navigation item **Id** (`Employees`, …) | **Family E** — shared `NavigationE2eSelectorsController` + hook map; `ShowNavigationItemAction` → **`NavigationComponentAdapter as DxAccordionAdapter`** → `ItemHeaderTextTemplate` wrapper | `NavigateUrl` fallback via `NavigationE2eSelectorSupport` | `nav-people-employees` (see People hooks today) |
| **MDI document tab** | ViewId + object key | TBD — `ITabbedMdiMainFormTemplate.TabsModel` | URL path | Not implemented |

Add rows only after one verified implementation.

---

## Decision tree

```text
Logon view?
  YES → CreateLogonWindowControllers + LogonViewE2eSelectorsController + Model.xafml backup

Detail BO member requested?
  IList<T> / aggregated collection?
    YES → STOP property hook; use TabbedGroup LayoutGroup Id (tab header hook)
  [NotMapped] computed OR [Browsable(false)] OR validation-only?
    YES → SKIP — not a direct detail field
  Optional gear-hidden scalar on [SupportsOptionalDetailFields] type?
    YES → SKIP unless developer explicitly named this member (see OPTIONAL_DETAIL_FIELDS.md)
  Required / always-visible direct scalar?
    YES → {Bo}DetailViewE2eSelectorsController + E2ePropertySelectorApplicator
         + optional ModelDefault on Module property
         Naming: {bo-kebab}-{member-kebab} (Person.FirstName → person-first-name)

Collection tab on detail (TabbedGroup "Tabs")?
  YES → PersonDetailViewE2eTabSelectorsController pattern (BlazorLayoutManager.ItemCreated)

Toolbar action?
  YES → Hook Action Id in ViewController; Model ActionDesign CustomCSSClassName optional

Sidebar navigation item (accordion)?  [Family E]
  YES → Extend NavigationE2eHooks map (Id + optional NavigateUrl)
       Reuse NavigationE2eSelectorsController + NavigationE2eSelectorSupport
       API: control.NavigationComponentAdapter as DxAccordionAdapter (NOT AccordionAdapter)
       Apply: CustomizeControl + IMainFormTemplate.ShowNavigationItemActionControl + ItemsChanged + ComponentCaptured
       Naming: nav-{target-kebab} from navigation item Id — not caption
       Verify: document.querySelector('.e2e-nav-…') on :5001 or Invoke-UiHookVerify -Scenario nav-*

Otherwise → stop; add reference row after spike; append learnings
```

See [SKILL.md § BO member scope](./SKILL.md#bo-member-scope--what-to-hook) for include/exclude table.

---

## Code patterns

### Shared field applicator

`Visa2026.Blazor.Server/Controllers/E2ePropertySelectorApplicator.cs` — sets `InputId`, `data-testid`, `e2e-*` on text/memo/masked editors and common DevExpress models (date, lookup, bool, image via reflection fallback). `E2eTextEditorSelectorApplicator` delegates here.

### Scalar member list (bulk hook)

`PersonE2eMemberHooks.cs` — explicit list of **in-scope** `Person` members (required / always-visible scalars; excludes collections, computed, hidden, and **optional gear-hidden** unless developer opted in). Pair with `PersonDetailViewE2eSelectorsController`. See [`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md).

### String field controller (sketch)

```csharp
View.CustomizeViewItemControl<StringPropertyEditor>(this, editor => { ... });
protected override void OnViewControlsCreated() {
    if (View.FindItem("MemberName") is BlazorPropertyEditorBase ed)
        E2ePropertySelectorApplicator.Apply(ed, "my-test-id");
}
```

### Logon registration

`Visa2026.Blazor.Server/BlazorApplication.cs`:

```csharp
protected override List<Controller> CreateLogonWindowControllers() {
    var result = base.CreateLogonWindowControllers();
    result.Add(new LogonViewE2eSelectorsController());
    return result;
}
```

### Layout tab (DevExpress official pattern)

```csharp
layoutManager = (BlazorLayoutManager)View.LayoutManager;
layoutManager.ItemCreated += (_, e) => {
    if (e.ModelLayoutElement.Id == "Tabs" && e.LayoutControlItem is DxFormLayoutTabPagesModel strip) {
        strip.SetAttribute("data-testid", "person-employee-tabs");
    }
    if (e.LayoutControlItem is DxFormLayoutTabPageModel tab
        && e.ModelLayoutElement.Id == "Passports") {
        tab.HeaderCssClass = "e2e-person-employee-tab-passports";
        tab.SetAttribute("data-testid", "person-employee-tab-passports");
    }
};
```

Source: [DevExpress tab layout example](https://github.com/DevExpress-Examples/xaf-how-to-access-a-tab-control-in-a-detail-view-layout).

### Model.xafml (CSS fallback)

```xml
<PropertyEditor Id="UserName" CustomCSSClassName="e2e-login-user-name" />
<Action Id="Logon" CustomCSSClassName="e2e-login-submit" />
<TabbedGroup Id="Tabs" CustomCSSClassName="e2e-person-employee-tabs">
  <LayoutGroup Id="Passports" CustomCSSClassName="e2e-person-employee-tab-passports" />
</TabbedGroup>
```

Model-only classes may land on **layout wrappers** — always verify clickable target in DevTools.

### Module BO attribute (layout class)

```csharp
[ModelDefault("CustomCSSClassName", "e2e-person-first-name")]
public virtual string FirstName { get; set; }
```

Pair with Blazor controller for `InputId` / `data-testid` on the actual input.

### Sidebar navigation (family E)

**Verified reference:** People group — `NavigationE2eHooks.cs`, `NavigationE2eSelectorsController.cs`, `NavigationE2eSelectorSupport.cs`.

**Add another nav group (Application, WorkPermit, …) — checklist (~15 min when controller is shared):**

1. Look up XAF navigation item **Ids** in `Model.xafml` / navigation model (not captions).
2. Add rows to hook map:
   ```csharp
   ["Application"] = "nav-application",
   ["Application_ListView"] = "nav-application-list",  // example — use real Id
   ```
3. For leaves, add **`TestIdsByNavigateUrl`** entries matching `href` (`Application_ListView`, …) from DevTools Elements.
4. Rebuild Blazor.Server; restart host.
5. DevTools (expand group if needed):
   ```javascript
   document.querySelector('.e2e-nav-application');
   document.querySelector('[data-testid="nav-application"]');
   ```
6. Optional: add `-Scenario nav-application` to `hooks-manifest.json` + registry row → promote to `docs/UI_TEST_HOOKS.md` after verify.

**Do not:** clone the WindowController per group; do not match nav by localized caption; do not use `deepQueryAll` unless a future DevExpress version moves wrappers into shadow (People wrappers are light DOM).

---

## DevTools verify snippets

**Login**

```javascript
document.querySelector('#login-user-name');
document.querySelector('#login-password');
document.querySelector('[data-testid="login-submit"]');
```

**Person employee tab**

```javascript
document.querySelector('[data-testid="person-employee-tab-passports"]')?.click();
document.querySelector('.e2e-person-employee-tab-passports')?.click();
```

**Person names**

```javascript
document.querySelector('#person-first-name');
document.querySelector('#person-last-name');
```

**Sidebar nav (People — verified)**

```javascript
document.querySelector('.e2e-nav-people-employees');
document.querySelector('.e2e-nav-people-family-members');
document.querySelector('[data-testid="nav-people-employees"]')?.click();
```

---

## Files to touch (checklist)

| Change | Files |
|--------|--------|
| New **scalar** field hooks | `{Bo}E2eMemberHooks.cs`, `{Bo}DetailViewE2eSelectorsController.cs`, optional `ModelDefault` on BO |
| Collection **tab** hooks | `*E2eTabSelectorsController.cs`, `Model.xafml` layout groups |
| **Sidebar nav** (family E) | Extend `NavigationE2eHooks.cs` map; `NavigationE2eSelectorsController.cs`, `NavigationE2eSelectorSupport.cs` |
| Logon | `LogonViewE2eSelectorsController.cs`, `BlazorApplication.cs`, `Model.xafml` |
| Verify (agent) | `scripts/local/Invoke-UiHookVerify.ps1`, `Visa2026.Blazor.Server/Properties/launchSettings.json` (**Visa2026 - Hook Verify (LocalDB)**), `tools/VerifyUiTestHooks/hooks-manifest.json` |
| Docs | `docs/UI_TEST_HOOKS.md`, `registry.md`, `learnings.md` |

---

## Out of scope (this skill)

- Running E2E suites (EasyTest, Playwright, Selenium)
- Web scraper tooling or CI automation
- Hooks on **collection properties**, **optional gear-hidden** scalars (unless explicitly requested), or **hidden/computed** members (see SKILL.md § BO member scope)
- Documenting access without DevTools verify
