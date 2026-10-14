using System;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Main-menu ids for the Report Dashboard folder and its category children.
/// The folder opens Overview. Each child opens that category on the same dashboard view.
/// </summary>
public static class ReportDashboardNavigationItems
{
    public const string RootId = "ReportDashboard";

    public const string ChildPrefix = "ReportDashboard_";

    public static string ChildId(ReportDashboardCategory category) => ChildPrefix + category;

    public static string PermissionPath(ReportDashboardCategory category) =>
        $@"Application/NavigationItems/Items/{RootId}/Items/{ChildId(category)}";

    public static bool TryParse(string? id, out bool showOverview, out ReportDashboardCategory category)
    {
        category = default;
        if (string.Equals(id, RootId, StringComparison.Ordinal))
        {
            showOverview = true;
            return true;
        }

        if (id != null
            && id.StartsWith(ChildPrefix, StringComparison.Ordinal)
            && Enum.TryParse(id[ChildPrefix.Length..], out category)
            && Enum.IsDefined(category))
        {
            showOverview = false;
            return true;
        }

        showOverview = true;
        return false;
    }

    public static bool TryRead(ChoiceActionItem? item, out bool showOverview, out ReportDashboardCategory category)
    {
        if (TryParse(item?.Id, out showOverview, out category))
            return true;

        if (item?.Data is IModelNavigationItem model)
            return TryParse(model.Id, out showOverview, out category);

        showOverview = true;
        category = default;
        return false;
    }

    public static ChoiceActionItem? FindChild(ChoiceActionItem? root, ReportDashboardCategory category)
    {
        if (root == null)
            return null;

        var id = ChildId(category);
        var direct = root.Items.Find(id);
        if (direct != null)
            return direct;

        foreach (ChoiceActionItem child in root.Items)
        {
            if (child.Data is IModelNavigationItem model
                && string.Equals(model.Id, id, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>Default-model captions. Culture overlays live in the localization xafml.</summary>
    public static string EnglishCaption(ReportDashboardCategory category) => category switch
    {
        ReportDashboardCategory.ApplicationViaMinistry => "Application (via ministry)",
        ReportDashboardCategory.ApplicationDirectMigration => "Application (direct migration)",
        ReportDashboardCategory.VisaExtension => "Visa",
        ReportDashboardCategory.Invitation => "Invitation",
        ReportDashboardCategory.Registration => "Registration",
        ReportDashboardCategory.WorkPermit => "Work Permit",
        ReportDashboardCategory.Travel => "Travel",
        ReportDashboardCategory.AddressOfResidence => "Address of Residence",
        ReportDashboardCategory.BorderZone => "Border Zone",
        ReportDashboardCategory.Passport => "Passport",
        ReportDashboardCategory.Education => "Education",
        ReportDashboardCategory.PositionHistory => "Position History",
        ReportDashboardCategory.Subcontractor => "Subcontractor",
        ReportDashboardCategory.MedicalRecord => "Medical Records",
        ReportDashboardCategory.IncompletePersons => "Persons with incomplete data",
        ReportDashboardCategory.PersonSearch => "Person search",
        _ => category.ToString()
    };
}

/// <summary>
/// Circuit-scoped selection read when the dashboard view is created.
/// </summary>
public sealed class ReportDashboardNavigationRequest
{
    public bool ShowOverview { get; private set; } = true;

    public ReportDashboardCategory Category { get; private set; } = ReportDashboardCategory.VisaExtension;

    public void SetOverview() => ShowOverview = true;

    public void SetCategory(ReportDashboardCategory category)
    {
        ShowOverview = false;
        Category = category;
    }

    public void Apply(bool showOverview, ReportDashboardCategory category)
    {
        if (showOverview)
            SetOverview();
        else
            SetCategory(category);
    }
}

/// <summary>Dashboard editor surface the navigation controller can retarget without a new tab.</summary>
public interface IReportDashboardNavigationTarget
{
    void ApplyNavigation(bool showOverview, ReportDashboardCategory category);
}
