using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Localization;

/// <summary>Tenant Turkmen lookup catalogs where <see cref="LookupBase.NameTm"/> is the sole admin-facing title.</summary>
internal static class TenantLookupTypes
{
    private static readonly Lazy<IReadOnlyList<Type>> TypesLazy = new(GetTypes, isThreadSafe: true);

    public static IReadOnlyList<Type> All => TypesLazy.Value;

    private static IReadOnlyList<Type> GetTypes() =>
    [
        typeof(Position),
        typeof(Specialty),
        typeof(EducationInstitution),
        typeof(Department),
        typeof(Subcontractor),
        typeof(ProjectContract),
        typeof(BorderZoneName),
        typeof(WorkPermittedLocationName),
        typeof(WorkPermitLocation),
        typeof(MovementPermitLocation),
        typeof(OrganizationType),
        typeof(ApplicationTypeFilter),
    ];
}
