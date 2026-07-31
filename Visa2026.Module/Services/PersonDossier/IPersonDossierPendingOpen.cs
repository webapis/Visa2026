using System;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Circuit-scoped pending person id for dossier opens from ListView row clicks.
/// Blazor AsyncLocal does not flow from JS interop into XAF view activation.
/// </summary>
public interface IPersonDossierPendingOpen
{
    Guid PersonId { get; set; }
}
