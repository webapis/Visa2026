using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Blazor.Templates;
using DevExpress.ExpressApp.Blazor.Templates.Navigation;
using DevExpress.ExpressApp.Blazor.Templates.Navigation.ActionControls;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Templates.ActionControls;
using Microsoft.AspNetCore.Components;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Adds stable <c>data-testid</c> / <c>.e2e-*</c> hooks to sidebar navigation items listed in <see cref="NavigationE2eHooks"/>.
/// </summary>
public sealed class NavigationE2eSelectorsController : WindowController
{
    private ShowNavigationItemActionControl? _navigationControl;
    private RenderFragment<IAccordionItemInfo>? _originalItemHeaderTextTemplate;
    private bool _componentCapturedHooked;

    public NavigationE2eSelectorsController()
    {
        TargetWindowType = WindowType.Main;
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        SingleChoiceAction? action = GetNavigationAction();
        if (action == null)
        {
            return;
        }

        action.CustomizeControl += OnShowNavigationItemCustomizeControl;
        action.ItemsChanged += OnNavigationItemsChanged;
        Frame.TemplateChanged += OnFrameTemplateChanged;

        TryApplyToExistingControl(action);
    }

    protected override void OnDeactivated()
    {
        SingleChoiceAction? action = GetNavigationAction();
        if (action != null)
        {
            action.CustomizeControl -= OnShowNavigationItemCustomizeControl;
            action.ItemsChanged -= OnNavigationItemsChanged;
        }

        Frame.TemplateChanged -= OnFrameTemplateChanged;

        if (_navigationControl?.NavigationComponentAdapter is DxAccordionAdapter adapter && _componentCapturedHooked)
        {
            adapter.ComponentCaptured -= OnAccordionComponentCaptured;
            _componentCapturedHooked = false;
        }

        _navigationControl = null;
        _originalItemHeaderTextTemplate = null;
        base.OnDeactivated();
    }

    private SingleChoiceAction? GetNavigationAction() =>
        Frame?.GetController<ShowNavigationItemController>()?.ShowNavigationItemAction;

    private void OnShowNavigationItemCustomizeControl(object? sender, CustomizeControlEventArgs e)
    {
        if (e.Control is ShowNavigationItemActionControl control && GetNavigationAction() is { } action)
        {
            ApplyNavigationSelectors(control, action);
        }
    }

    private void OnNavigationItemsChanged(object? sender, ItemsChangedEventArgs e)
    {
        if (_navigationControl is not null && GetNavigationAction() is { } action)
        {
            ApplyNavigationSelectors(_navigationControl, action);
        }
        else if (GetNavigationAction() is { } fallbackAction)
        {
            TryApplyToExistingControl(fallbackAction);
        }
    }

    private void OnFrameTemplateChanged(object? sender, EventArgs e)
    {
        if (GetNavigationAction() is { } action)
        {
            TryApplyToExistingControl(action);
        }
    }

    private void OnAccordionComponentCaptured(object? sender, ComponentCapturedEventArgs<DxAccordion> e)
    {
        if (_navigationControl is not null && GetNavigationAction() is { } action)
        {
            ApplyNavigationSelectors(_navigationControl, action);
        }
    }

    private void TryApplyToExistingControl(SingleChoiceAction action)
    {
        if (Frame?.Template is not IMainFormTemplate mainFormTemplate)
        {
            return;
        }

        if (mainFormTemplate.ShowNavigationItemActionControl is not ShowNavigationItemActionControl control)
        {
            return;
        }

        ApplyNavigationSelectors(control, action);
    }

    private void ApplyNavigationSelectors(ShowNavigationItemActionControl control, SingleChoiceAction action)
    {
        _navigationControl = control;

        if (control.NavigationComponentAdapter is not DxAccordionAdapter accordionAdapter)
        {
            return;
        }

        if (!_componentCapturedHooked)
        {
            accordionAdapter.ComponentCaptured += OnAccordionComponentCaptured;
            _componentCapturedHooked = true;
        }

        _originalItemHeaderTextTemplate ??= accordionAdapter.ComponentModel.ItemHeaderTextTemplate;
        RenderFragment<IAccordionItemInfo>? originalTemplate = _originalItemHeaderTextTemplate;

        accordionAdapter.ComponentModel.ItemHeaderTextTemplate = context =>
        {
            IList<ChoiceActionItem> actionItems = action.Items?.ToList() ?? [];
            IList<object> rootMenuItems = NavigationE2eSelectorSupport.GetRootMenuItems(
                accordionAdapter.ComponentModel.Data);

            string? testId = NavigationE2eSelectorSupport.FindTestIdForDataItem(
                context.DataItem,
                actionItems,
                rootMenuItems,
                NavigationE2eHooks.TestIdsByNavigationItemId);

            if (string.IsNullOrEmpty(testId) || originalTemplate is null)
            {
                return originalTemplate?.Invoke(context) ?? (_ => { });
            }

            RenderFragment inner = originalTemplate(context);
            return builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", $"e2e-{testId}");
                builder.AddAttribute(2, "data-testid", testId);
                builder.AddContent(3, inner);
                builder.CloseElement();
            };
        };
    }
}
