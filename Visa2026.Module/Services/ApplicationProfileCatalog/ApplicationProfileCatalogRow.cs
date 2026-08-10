using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.OfficerShell;

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
    public int StagedUses { get; init; }
    public int InProcessUses { get; init; }
    public string TemplateFamilyKey { get; init; } = OfficerShellTemplateFamily.Invitation;
    /// <summary>active · locked · draft — officer-shell template catalog parity.</summary>
    public string StatusKey { get; init; } = "active";
    public string ActionFamilyLabel { get; init; } = string.Empty;
    public string ProgressRouteLabel { get; init; } = string.Empty;
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