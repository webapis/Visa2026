# UI test hooks — reference

Mechanism matrix and copy-paste patterns. Canonical workflow: [SKILL.md](./SKILL.md).

---

## Element type → mechanism

| UI element | Stable id source | Primary mechanism | Fallback | Verified example |
|------------|------------------|-------------------|----------|------------------|
| **String BO field** | BO member name | `ViewController` + `E2eTextEditorSelectorApplicator` (`InputId`, `data-testid`, `CssClass`) | `[ModelDefault("CustomCSSClassName")]` on property | `Person.FirstName` → `#person-first-name` |
| **Logon UserName / Password** | `UserName`, `Password` | `LogonViewE2eSelectorsController` + **`CreateLogonWindowControllers`** | `Model.xafml` layout classes | `#login-user-name`, `#login-password` |
| **Logon button** | Action Id `Logon` | `Action.CustomizeControl` → `DxToolbarItemSimpleActionControl` | `Model.xafml` `ActionDesign` | `[data-testid="login-submit"]` |
| **Layout tab page** | `LayoutGroup` Id under `TabbedGroup` | `BlazorLayoutManager.ItemCreated` → `DxFormLayoutTabPageModel.HeaderCssClass` + `SetAttribute` | `Model.xafml` `CustomCSSClassName` on layout group | Passports → `person-employee-tab-passports` |
| **Layout tab strip** | `TabbedGroup` Id (e.g. `Tabs`) | `ItemCreated` → `DxFormLayoutTabPagesModel` | `Model.xafml` on `TabbedGroup` | `person-employee-tabs` |
| **Standard action** | Action Id | `CustomizeControl` on toolbar item + optional `Model.xafml` | — | Log In (see logon) |
| **Custom property editor** | Member + editor alias | Dedicated `ViewController` + component model | — | TBD (e.g. ApplicationType quick code) |
| **ListView / grid cell** | ListView Id + column | TBD — `DxGridListEditor` / column model | — | Not implemented |
| **MDI document tab** | ViewId + object key | TBD — `ITabbedMdiMainFormTemplate.TabsModel` | URL path | Not implemented |

Add rows only after one verified implementation.

---

## Decision tree

```text
Logon view?
  YES → CreateLogonWindowControllers + LogonViewE2eSelectorsController + Model.xafml backup

Person/detail string field?
  YES → PersonDetailViewE2eSelectorsController pattern + E2eTextEditorSelectorApplicator

Collection tab on detail (TabbedGroup "Tabs")?
  YES → PersonDetailViewE2eTabSelectorsController pattern (BlazorLayoutManager.ItemCreated)

Toolbar action?
  YES → Hook Action Id in ViewController; Model ActionDesign CustomCSSClassName optional

Otherwise → stop; add reference row after spike; append learnings
```

---

## Code patterns

### Shared text field applicator

`Visa2026.Blazor.Server/Controllers/E2eTextEditorSelectorApplicator.cs` — sets `InputId`, `data-testid`, `e2e-*` on `DxTextBoxModel` / masked / memo.

### String field controller (sketch)

```csharp
View.CustomizeViewItemControl<StringPropertyEditor>(this, editor => { ... });
protected override void OnViewControlsCreated() {
    if (View.FindItem("MemberName") is BlazorPropertyEditorBase ed)
        E2eTextEditorSelectorApplicator.Apply(ed, "my-test-id");
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

---

## Files to touch (checklist)

| Change | Files |
|--------|--------|
| New field hooks | `*E2eSelectorsController.cs`, optional `Person.cs` ModelDefault |
| Logon | `LogonViewE2eSelectorsController.cs`, `BlazorApplication.cs`, `Model.xafml` |
| Tabs | `PersonDetailViewE2eTabSelectorsController.cs`, `Model.xafml` layout groups |
| Docs | `docs/UI_TEST_HOOKS.md`, `registry.md`, `learnings.md`, `tools/VerifyUiTestHooks/hooks-manifest.json` |

---

## Out of scope (this skill)

- Running E2E suites (EasyTest, Playwright, Selenium)
- Web scraper tooling or CI automation
- Hooks on every BO member upfront
- Documenting access without DevTools verify
