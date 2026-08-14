using System.Collections.Generic;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public interface IApplicationProfilePickerQueryService
{
    IReadOnlyList<ApplicationProfilePickerRow> GetProfiles(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceProgressRouteKind? progressRouteFilter,
        ApplicationProfileInstance? applicabilityProbe = null,
        Guid? seedPersonId = null);
}
