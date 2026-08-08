using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>
/// Scoped Blazor session for the profile wizard PropertyEditor (live ObjectSpace editing).
/// </summary>
public interface IApplicationProfileWizardSession
{
    IObjectSpace? ObjectSpace { get; set; }

    XafApplication? Application { get; set; }

    Guid ApplicationProfileId { get; set; }

    ApplicationProfile? GetProfile();
}
