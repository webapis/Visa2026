#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Review Add-placeholder list: same catalog for every profile and Header/Row save-scope.
/// Convert stays pack- and scope-gated.
/// </summary>
public static class ScanPlaceholderLibrary
{
    public static ApplicationProfilePlaceholderSetQuery Query(
        ApplicationProfile profile,
        ApplicationProfileTemplateKind kind) =>
        new()
        {
            Profile = profile,
            DataScope = ApplicationProfileTemplateDataScope.Both,
            TemplateKind = kind,
            OfferFullLibrary = true,
        };
}