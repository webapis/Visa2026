using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

public sealed class ApplicationProfileCatalogRow
{
    public Guid ProfileId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? SelectionCode { get; init; }
    public ApplicationProfileActionFamily ActionFamily { get; init; }
    public ApplicationProgressRouteKind ProgressRoute { get; init; }
    public bool IsActive { get; init; }
    public bool IsConfigLocked { get; init; }
    public int LinkedApplicationCount { get; init; }

    /// <summary>Compact left-rail caption, e.g. "201 - Gulluk Pasporty…".</summary>
    public string RailLabel
    {
        get
        {
            var prefix = !string.IsNullOrWhiteSpace(SelectionCode) ? SelectionCode!.Trim() : Code;
            if (string.IsNullOrWhiteSpace(prefix))
                return Name;
            return string.IsNullOrWhiteSpace(Name) ? prefix : $"{prefix} - {Name}";
        }
    }

    public string MetaLine
    {
        get
        {
            var parts = new[]
            {
                Code,
                string.IsNullOrWhiteSpace(SelectionCode) ? null : SelectionCode,
                ApplicationProfilePickerDisplayHelper.FormatActionFamily(ActionFamily),
                ApplicationProfilePickerDisplayHelper.FormatProgressRoute(ProgressRoute),
            };
            return string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
}