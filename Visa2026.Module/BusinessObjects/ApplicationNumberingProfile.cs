using System;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant singleton: application number prefix, format template, seed, and padding.
/// Separate from <see cref="CompanyProfile"/> so company identity and numbering can vary per deployment.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Organization")]
[DisplayName("Application Numbering")]
[DefaultProperty(nameof(AppNumberPrefix))]
[ImageName("BO_Number")]
public class ApplicationNumberingProfile : BaseObject
{
    public const string DefaultProfileName = "Default";
    public const int DefaultApplicationNumberPadding = 4;
    public const int DefaultApplicationNumberSeed = 0;

    [Browsable(false)]
    [RuleRequiredField(DefaultContexts.Save)]
    public virtual string Name { get; set; }

    [XafDisplayName("App Number Prefix")]
    public virtual string AppNumberPrefix { get; set; }

    [XafDisplayName("App Number Format")]
    [ToolTip("Tokens: {PREFIX} {YEAR} {YEAR2} {MONTH} {MONTH2} {NUMBER}. Example: {PREFIX}{YEAR}-{NUMBER} → TRM-2026-001")]
    public virtual string AppNumberFormat { get; set; }

    [XafDisplayName("App Number Seed")]
    [ToolTip("Last application number used before this system; the next generated number continues from this value.")]
    public virtual int ApplicationNumberSeed { get; set; }

    [XafDisplayName("App Number Padding")]
    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual int ApplicationNumberPadding { get; set; }

    public override void OnCreated()
    {
        base.OnCreated();
        Name = DefaultProfileName;
        ApplicationNumberSeed = DefaultApplicationNumberSeed;
        ApplicationNumberPadding = DefaultApplicationNumberPadding;
        AppNumberFormat = "{PREFIX}{YEAR}-{NUMBER}";
    }

    public static ApplicationNumberingProfile? TryGetInstance(IObjectSpace objectSpace) =>
        OrganizationSingletonHelper.TryGet(
            objectSpace,
            (ApplicationNumberingProfile p) => p.Name,
            tieBreaker: rows => rows
                .OrderByDescending(p => p.Name == DefaultProfileName)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .First());

    public static ApplicationNumberingProfile GetOrCreateInstance(IObjectSpace objectSpace) =>
        TryGetInstance(objectSpace) ?? objectSpace.CreateObject<ApplicationNumberingProfile>();
}
