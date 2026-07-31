using System;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Scoped pending person id for dossier opens (one Blazor circuit).
/// </summary>
public sealed class PersonDossierPendingOpen : IPersonDossierPendingOpen
{
    public Guid PersonId { get; set; }
}
