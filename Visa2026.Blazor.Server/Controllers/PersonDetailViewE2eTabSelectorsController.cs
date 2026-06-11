using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Layout;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stable selectors on Person detail layout tabs (collection tabs under TabbedGroup "Tabs").
/// Uses <see cref="BlazorLayoutManager.ItemCreated"/> (DevExpress layout tab access pattern).
/// </summary>
public sealed class PersonDetailViewE2eTabSelectorsController : ViewController<DetailView>
{
    private const string CollectionTabsGroupId = "Tabs";

    private static readonly string[] TargetViewIds =
    {
        "Person_DetailView_Employee",
        "Person_DetailView",
        "Person_DetailView_FamilyMember",
        "Person_DetailView_TemporaryVisitor",
    };

    private static readonly Dictionary<string, string> TabTestIds =
        new(StringComparer.Ordinal)
        {
            ["Educations"] = "person-employee-tab-educations",
            ["Passports"] = "person-employee-tab-passports",
            ["PositionHistory"] = "person-employee-tab-position-history",
            ["MedicalRecords"] = "person-employee-tab-medical-records",
            ["AddressesOfResidence"] = "person-employee-tab-addresses-of-residence",
            ["Documents"] = "person-employee-tab-documents",
            ["FamilyRelationDocuments"] = "person-employee-tab-family-relation-documents",
            ["TravelHistories"] = "person-employee-tab-travel-histories",
            ["WorkDuties"] = "person-employee-tab-work-duties",
            ["Salaries"] = "person-employee-tab-salaries",
            ["WorkPermitItems"] = "person-employee-tab-work-permit-items",
            ["FamilyMembers"] = "person-employee-tab-family-members",
            ["InvitationItems"] = "person-employee-tab-invitation-items",
        };

    private BlazorLayoutManager? _layoutManager;

    public PersonDetailViewE2eTabSelectorsController()
    {
        TargetObjectType = typeof(Person);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        if (!IsTargetPersonDetailView())
        {
            return;
        }

        AttachLayoutManager();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        if (!IsTargetPersonDetailView())
        {
            return;
        }

        AttachLayoutManager();
    }

    protected override void OnDeactivated()
    {
        if (_layoutManager != null)
        {
            _layoutManager.ItemCreated -= LayoutManager_ItemCreated;
            _layoutManager = null;
        }

        base.OnDeactivated();
    }

    private bool IsTargetPersonDetailView() =>
        View?.Id != null && TargetViewIds.Contains(View.Id);

    private void AttachLayoutManager()
    {
        if (View?.LayoutManager is not BlazorLayoutManager layoutManager)
        {
            return;
        }

        if (_layoutManager == layoutManager)
        {
            return;
        }

        if (_layoutManager != null)
        {
            _layoutManager.ItemCreated -= LayoutManager_ItemCreated;
        }

        _layoutManager = layoutManager;
        _layoutManager.ItemCreated += LayoutManager_ItemCreated;
    }

    private void LayoutManager_ItemCreated(object? sender, BlazorLayoutManager.ItemCreatedEventArgs e)
    {
        if (e.ModelLayoutElement.Id == CollectionTabsGroupId
            && e.LayoutControlItem is DxFormLayoutTabPagesModel tabStrip)
        {
            tabStrip.CssClass = "e2e-person-employee-tabs";
            tabStrip.SetAttribute("data-testid", "person-employee-tabs");
            return;
        }

        if (e.LayoutControlItem is not DxFormLayoutTabPageModel tabPage)
        {
            return;
        }

        if (!TabTestIds.TryGetValue(e.ModelLayoutElement.Id, out string? testId))
        {
            return;
        }

        tabPage.HeaderCssClass = $"e2e-{testId}";
        tabPage.CssClass = $"e2e-{testId}-content";
        tabPage.SetAttribute("data-testid", testId);
    }
}
