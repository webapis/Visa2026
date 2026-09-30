using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

/// <summary>
/// Which shared approval-leg chain the create picker highlights.
/// Chains are one catalog for every via-ministry profile, so a chain id left over
/// from another profile (or from the seed default) is not this profile's choice.
/// </summary>
public static class ApplicationProfilePickerVersionSelection
{
    public static Guid Resolve(
        Guid currentVersionId,
        bool explicitlyChosen,
        IEnumerable<ApplicationProfilePickerVersionOption>? versions)
    {
        var list = versions?.Where(v => v != null && v.VersionId != Guid.Empty).ToList()
            ?? new List<ApplicationProfilePickerVersionOption>();

        if (explicitlyChosen && list.Any(v => v.VersionId == currentVersionId))
            return currentVersionId;

        var savedDefault = list.FirstOrDefault(v => v.IsDefault);
        if (savedDefault != null)
            return savedDefault.VersionId;

        return list.Count > 0 ? list[0].VersionId : Guid.Empty;
    }
}
