using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Application-wide days allowed to issue visas after the work-permit letter
/// on Wiza we Iş Rugsatnamasyny Uzaltmak, and how many days before that the counter warns.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Configuration")]
[DisplayName("Visa issue window")]
[ModelDefault("Caption", "Wiza berilmeli möhlet")]
[RuleCriteria(
    "VisaIssueWindowSettings_WarningWithinWindow",
    DefaultContexts.Save,
    "WarningDays <= WindowDays",
    CustomMessageTemplate = "Warning days cannot exceed the visa issue window.")]
public class VisaIssueWindowSettings : BaseObject
{
    public const int DefaultWindowDays = VisaIssueWindowCalculator.DefaultWindowDays;
    public const int DefaultWarningDays = VisaIssueWindowCalculator.DefaultWarningDays;

    [XafDisplayName("Wiza berilmeli möhlet (gün)")]
    [ToolTip("Calendar days after the work-permit letter date within which the new visas should be issued.")]
    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual int WindowDays { get; set; }

    [XafDisplayName("Duýduryş öňünden (gün)")]
    [ToolTip("The counter and the Wiza pill turn the warning color when this many days or fewer remain, including day 0.")]
    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual int WarningDays { get; set; }

    public override void OnCreated()
    {
        base.OnCreated();
        WindowDays = DefaultWindowDays;
        WarningDays = DefaultWarningDays;
    }

    public static VisaIssueWindowSettings? TryGetInstance(IObjectSpace objectSpace) =>
        OrganizationSingletonHelper.TryGet(objectSpace, (VisaIssueWindowSettings _) => "Visa issue window");

    public static VisaIssueWindowSettings GetOrCreateInstance(IObjectSpace objectSpace) =>
        TryGetInstance(objectSpace) ?? objectSpace.CreateObject<VisaIssueWindowSettings>();

    public static (int WindowDays, int WarningDays) Current(IObjectSpace? objectSpace)
    {
        VisaIssueWindowSettings? row = null;
        if (objectSpace != null)
        {
            try
            {
                row = TryGetInstance(objectSpace);
            }
            catch (Exception ex) when (IsSecurityDenial(ex))
            {
                row = null;
            }
        }

        var window = row?.WindowDays > 0 ? row.WindowDays : DefaultWindowDays;
        var warning = row?.WarningDays > 0 ? row.WarningDays : DefaultWarningDays;
        return (window, VisaIssueWindowCalculator.NormalizeWarningDays(warning, window));
    }

    private static bool IsSecurityDenial(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            var name = current.GetType().FullName ?? string.Empty;
            if (name.Contains("Security", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
