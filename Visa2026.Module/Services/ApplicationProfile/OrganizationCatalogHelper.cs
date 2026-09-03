#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Multi-row Company / Signatory / Representative catalogs (not singletons).
/// Tenant Default pre-fills the next Application Profile Instance.
/// Instances store live FKs; merge reads the selected rows.
/// Keep this type in BusinessObjects — Services.ApplicationProfile makes ApplicationProfile a namespace.
/// </summary>
public static class OrganizationCatalogHelper
{
    public const string Company = "company";
    public const string Signatory = "signatory";
    public const string Representative = "representative";

    public static CompanyProfile? TryGetDefaultCompany(IObjectSpace? objectSpace) =>
        TryGetDefault(objectSpace, (CompanyProfile p) => p.Name);

    public static AuthorizedSignatory? TryGetDefaultSignatory(IObjectSpace? objectSpace) =>
        TryGetDefault(objectSpace, (AuthorizedSignatory p) => p.FullName);

    public static AuthorizedRepresentative? TryGetDefaultRepresentative(IObjectSpace? objectSpace) =>
        TryGetDefault(objectSpace, (AuthorizedRepresentative p) => p.FullName);

    public static T? TryGetDefault<T>(IObjectSpace? objectSpace, Func<T, string?> keySelector)
        where T : BaseObject
    {
        if (objectSpace == null)
            return null;

        var populated = objectSpace.GetObjectsQuery<T>()
            .AsEnumerable()
            .Where(x => !string.IsNullOrWhiteSpace(keySelector(x)))
            .ToList();
        if (populated.Count == 0)
            return null;

        return populated.FirstOrDefault(x => IsDefaultFlag(x))
            ?? populated.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase).First();
    }

    public static IReadOnlyList<OrganizationCatalogOption> ListCompanies(IObjectSpace? objectSpace) =>
        List(objectSpace, (CompanyProfile p) => DisplayCompany(p), p => p.IsDefault);

    public static IReadOnlyList<OrganizationCatalogOption> ListSignatories(IObjectSpace? objectSpace) =>
        List(objectSpace, (AuthorizedSignatory p) => p.FullName, p => p.IsDefault);

    public static IReadOnlyList<OrganizationCatalogOption> ListRepresentatives(IObjectSpace? objectSpace) =>
        List(objectSpace, (AuthorizedRepresentative p) => p.FullName, p => p.IsDefault);

    public static IReadOnlyList<OrganizationCatalogRow> ListCompanyRows(IObjectSpace? objectSpace) =>
        ListRows(objectSpace, (CompanyProfile p) => new OrganizationCatalogRow
        {
            Id = p.ID,
            Kind = Company,
            Name = p.Name?.Trim() ?? string.Empty,
            Code = p.Code?.Trim() ?? string.Empty,
            Title = string.Empty,
            IsDefault = p.IsDefault,
        }, r => r.Name);

    public static IReadOnlyList<OrganizationCatalogRow> ListSignatoryRows(IObjectSpace? objectSpace) =>
        ListRows(objectSpace, (AuthorizedSignatory p) => new OrganizationCatalogRow
        {
            Id = p.ID,
            Kind = Signatory,
            Name = p.FullName?.Trim() ?? string.Empty,
            Code = string.Empty,
            Title = p.PositionTitleTm?.Trim() ?? string.Empty,
            IsDefault = p.IsDefault,
        }, r => r.Name);

    public static IReadOnlyList<OrganizationCatalogRow> ListRepresentativeRows(IObjectSpace? objectSpace) =>
        ListRows(objectSpace, (AuthorizedRepresentative p) => new OrganizationCatalogRow
        {
            Id = p.ID,
            Kind = Representative,
            Name = p.FullName?.Trim() ?? string.Empty,
            Code = string.Empty,
            Title = p.PositionTitleTm?.Trim() ?? string.Empty,
            IsDefault = p.IsDefault,
        }, r => r.Name);

    public static IReadOnlyList<OrganizationCatalogRow> FilterRows(
        IReadOnlyList<OrganizationCatalogRow> rows,
        string? search)
    {
        if (rows == null || rows.Count == 0)
            return rows ?? Array.Empty<OrganizationCatalogRow>();

        var q = (search ?? string.Empty).Trim();
        if (q.Length == 0)
            return rows;

        return rows
            .Where(r =>
                r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.Code.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || r.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public static int EnsureOneDefault(IObjectSpace objectSpace)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        var changed = 0;
        changed += EnsureOneDefault(objectSpace, (CompanyProfile p) => p.Name, (p, v) => p.IsDefault = v);
        changed += EnsureOneDefault(objectSpace, (AuthorizedSignatory p) => p.FullName, (p, v) => p.IsDefault = v);
        changed += EnsureOneDefault(objectSpace, (AuthorizedRepresentative p) => p.FullName, (p, v) => p.IsDefault = v);
        return changed;
    }

    public static bool TryMakeDefault(IObjectSpace objectSpace, string kind, Guid id, out string? error)
    {
        error = null;
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (id == Guid.Empty)
        {
            error = "Select a catalog record first.";
            return false;
        }

        switch (kind)
        {
            case Company:
                return MakeDefault(objectSpace, objectSpace.GetObjectByKey<CompanyProfile>(id), out error);
            case Signatory:
                return MakeDefault(objectSpace, objectSpace.GetObjectByKey<AuthorizedSignatory>(id), out error);
            case Representative:
                return MakeDefault(objectSpace, objectSpace.GetObjectByKey<AuthorizedRepresentative>(id), out error);
            default:
                error = "Unknown organization catalog.";
                return false;
        }
    }

    public static void AssignDefaultsIfEmpty(ApplicationProfileInstance application, IObjectSpace? objectSpace = null)
    {
        ArgumentNullException.ThrowIfNull(application);
        var os = objectSpace ?? ObjectSpaceHelper.Get(application);
        if (os == null)
            return;

        if (application.OrganizationCompany == null && application.OrganizationCompanyId == null)
            application.OrganizationCompany = TryGetDefaultCompany(os);
        if (application.OrganizationSignatory == null && application.OrganizationSignatoryId == null)
            application.OrganizationSignatory = TryGetDefaultSignatory(os);
        if (application.OrganizationRepresentative == null && application.OrganizationRepresentativeId == null)
            application.OrganizationRepresentative = TryGetDefaultRepresentative(os);
    }

    public static void Assign(
        ApplicationProfileInstance application,
        IObjectSpace objectSpace,
        Guid? companyId,
        Guid? signatoryId,
        Guid? representativeId)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(objectSpace);

        application.OrganizationCompany = companyId is Guid cid && cid != Guid.Empty
            ? objectSpace.GetObjectByKey<CompanyProfile>(cid)
            : TryGetDefaultCompany(objectSpace);
        application.OrganizationSignatory = signatoryId is Guid sid && sid != Guid.Empty
            ? objectSpace.GetObjectByKey<AuthorizedSignatory>(sid)
            : TryGetDefaultSignatory(objectSpace);
        application.OrganizationRepresentative = representativeId is Guid rid && rid != Guid.Empty
            ? objectSpace.GetObjectByKey<AuthorizedRepresentative>(rid)
            : TryGetDefaultRepresentative(objectSpace);
    }

    public static bool TryAssign(
        ApplicationProfileInstance application,
        IObjectSpace objectSpace,
        string kind,
        Guid? id,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(objectSpace);
        error = null;
        if (string.IsNullOrWhiteSpace(kind))
        {
            error = "Missing organization field.";
            return false;
        }

        switch (kind)
        {
            case Company:
                application.OrganizationCompany = id is Guid cid && cid != Guid.Empty
                    ? objectSpace.GetObjectByKey<CompanyProfile>(cid)
                    : null;
                if (id is Guid missingCompany && missingCompany != Guid.Empty && application.OrganizationCompany == null)
                {
                    error = "Company not found.";
                    return false;
                }
                return true;
            case Signatory:
                application.OrganizationSignatory = id is Guid sid && sid != Guid.Empty
                    ? objectSpace.GetObjectByKey<AuthorizedSignatory>(sid)
                    : null;
                if (id is Guid missingSignatory && missingSignatory != Guid.Empty && application.OrganizationSignatory == null)
                {
                    error = "Authorized Signatory not found.";
                    return false;
                }
                return true;
            case Representative:
                application.OrganizationRepresentative = id is Guid rid && rid != Guid.Empty
                    ? objectSpace.GetObjectByKey<AuthorizedRepresentative>(rid)
                    : null;
                if (id is Guid missingRep && missingRep != Guid.Empty && application.OrganizationRepresentative == null)
                {
                    error = "Authorized Representative not found.";
                    return false;
                }
                return true;
            default:
                error = "Unknown organization field.";
                return false;
        }
    }

    public static int BackfillUnassigned(IObjectSpace objectSpace)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        EnsureOneDefault(objectSpace);
        var filled = 0;
        foreach (var application in objectSpace.GetObjectsQuery<ApplicationProfileInstance>())
        {
            var beforeCompany = application.OrganizationCompanyId ?? application.OrganizationCompany?.ID;
            var beforeSignatory = application.OrganizationSignatoryId ?? application.OrganizationSignatory?.ID;
            var beforeRep = application.OrganizationRepresentativeId ?? application.OrganizationRepresentative?.ID;
            AssignDefaultsIfEmpty(application, objectSpace);
            var afterCompany = application.OrganizationCompanyId ?? application.OrganizationCompany?.ID;
            var afterSignatory = application.OrganizationSignatoryId ?? application.OrganizationSignatory?.ID;
            var afterRep = application.OrganizationRepresentativeId ?? application.OrganizationRepresentative?.ID;
            if (beforeCompany != afterCompany || beforeSignatory != afterSignatory || beforeRep != afterRep)
                filled++;
        }

        return filled;
    }

    public static string DisplayCompany(CompanyProfile? company)
    {
        if (company == null)
            return string.Empty;
        var name = company.Name?.Trim() ?? string.Empty;
        var code = company.Code?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(code))
            return name;
        if (string.IsNullOrEmpty(name))
            return code;
        return $"{name} ({code})";
    }

    public static string DisplayPerson(string? fullName, string? title)
    {
        var name = fullName?.Trim() ?? string.Empty;
        var position = title?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(position))
            return name;
        if (string.IsNullOrEmpty(name))
            return position;
        return $"{name} — {position}";
    }

    private static IReadOnlyList<OrganizationCatalogRow> ListRows<T>(
        IObjectSpace? objectSpace,
        Func<T, OrganizationCatalogRow> map,
        Func<OrganizationCatalogRow, string> sortKey)
        where T : BaseObject
    {
        if (objectSpace == null)
            return Array.Empty<OrganizationCatalogRow>();

        return objectSpace.GetObjectsQuery<T>()
            .AsEnumerable()
            .Select(map)
            .OrderByDescending(r => r.IsDefault)
            .ThenBy(sortKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<OrganizationCatalogOption> List<T>(
        IObjectSpace? objectSpace,
        Func<T, string?> title,
        Func<T, bool> isDefault)
        where T : BaseObject
    {
        if (objectSpace == null)
            return Array.Empty<OrganizationCatalogOption>();

        return objectSpace.GetObjectsQuery<T>()
            .AsEnumerable()
            .Select(item => new OrganizationCatalogOption
            {
                Id = item.ID,
                DisplayName = string.IsNullOrWhiteSpace(title(item)) ? item.ID.ToString() : title(item)!.Trim(),
                IsDefault = isDefault(item),
            })
            .OrderByDescending(o => o.IsDefault)
            .ThenBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int EnsureOneDefault<T>(
        IObjectSpace objectSpace,
        Func<T, string?> keySelector,
        Action<T, bool> setDefault)
        where T : BaseObject
    {
        var populated = objectSpace.GetObjectsQuery<T>()
            .AsEnumerable()
            .Where(x => !string.IsNullOrWhiteSpace(keySelector(x)))
            .ToList();
        if (populated.Count == 0)
            return 0;

        var defaults = populated.Where(x => IsDefaultFlag(x)).ToList();
        if (defaults.Count == 1)
            return 0;

        if (defaults.Count > 1)
        {
            var keeper = defaults.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase).First();
            foreach (var row in defaults)
            {
                if (ReferenceEquals(row, keeper))
                    continue;
                setDefault(row, false);
            }
            return defaults.Count - 1;
        }

        var first = populated.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase).First();
        setDefault(first, true);
        return 1;
    }

    private static bool MakeDefault<T>(IObjectSpace objectSpace, T? selected, out string? error)
        where T : BaseObject
    {
        error = null;
        if (selected == null)
        {
            error = "Catalog record not found.";
            return false;
        }

        foreach (var row in objectSpace.GetObjectsQuery<T>())
            SetDefaultFlag(row, ReferenceEquals(row, selected));

        objectSpace.SetModified(selected);
        if (objectSpace.IsModified)
            objectSpace.CommitChanges();
        return true;
    }

    private static bool IsDefaultFlag(object row) =>
        row switch
        {
            CompanyProfile company => company.IsDefault,
            AuthorizedSignatory signatory => signatory.IsDefault,
            AuthorizedRepresentative representative => representative.IsDefault,
            _ => false,
        };

    private static void SetDefaultFlag(object row, bool value)
    {
        switch (row)
        {
            case CompanyProfile company:
                company.IsDefault = value;
                break;
            case AuthorizedSignatory signatory:
                signatory.IsDefault = value;
                break;
            case AuthorizedRepresentative representative:
                representative.IsDefault = value;
                break;
        }
    }
}

public sealed class OrganizationCatalogOption
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

public sealed class OrganizationCatalogRow
{
    public Guid Id { get; init; }
    public string Kind { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public bool IsDefault { get; init; }

    public string DisplayName => OrganizationCatalogHelper.DisplayPerson(Name, Title);
}