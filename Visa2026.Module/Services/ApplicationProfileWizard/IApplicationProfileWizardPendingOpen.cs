using System;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

public interface IApplicationProfileWizardPendingOpen
{
    Guid ApplicationProfileId { get; set; }
}
