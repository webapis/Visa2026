using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Add / rename lookup records from the person intake wizard dialog (instead of the XAF DetailView popup).
/// Shared catalogs (<see cref="GlobalLookupCatalogBase"/>) can be added but not renamed.
/// </summary>
public static class PersonIntakeLookupEditor
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public sealed class Values
    {
        public string Name { get; set; } = string.Empty;
        public string ReverseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Types whose only required input is a name. Others (City needs a Region; Hotel, Hospital,
    /// Lodging, OtherSite need address data) stay on the XAF DetailView.
    /// </summary>
    public static bool IsSupported(Type? type) =>
        type != null
        && type != typeof(City)
        && (typeof(LookupBase).IsAssignableFrom(type) || type == typeof(ActualPosition));

    public static bool IsSharedCatalog(Type? type) =>
        type != null && typeof(GlobalLookupCatalogBase).IsAssignableFrom(type);

    public static bool CanRename(Type? type) => IsSupported(type) && !IsSharedCatalog(type);

    public static bool HasReverseName(Type? type) => type == typeof(Relationship);

    public static bool HasContractFields(Type? type) => type == typeof(ProjectContract);

    public static bool IsGranted(IObjectSpace? objectSpace, Type? type, string operation)
    {
        if (objectSpace == null || type == null)
            return false;
        try
        {
            return SecuritySystem.Instance == null
                || SecuritySystem.IsGranted(new PermissionRequest(objectSpace, type!, operation));
        }
        catch
        {
            return false;
        }
    }

    public static Values Load(IObjectSpace objectSpace, Type type, Guid id)
    {
        var values = new Values();
        var item = objectSpace.GetObjectByKey(type, id);
        switch (item)
        {
            case ActualPosition actual:
                values.Name = actual.Name ?? string.Empty;
                break;
            case LookupBase lookup:
                values.Name = lookup.NameTm ?? string.Empty;
                break;
        }

        if (item is Relationship relationship)
            values.ReverseName = relationship.ReverseNameTm ?? string.Empty;
        if (item is ProjectContract contract)
        {
            values.Description = contract.Description ?? string.Empty;
            values.IsActive = contract.IsActive;
        }

        return values;
    }

    /// <summary>Returns an officer-facing message, or null when the values can be saved.</summary>
    public static string? Validate(IObjectSpace objectSpace, Type type, Guid? excludeId, Values values)
    {
        var name = CaseAddressAutocomplete.NormalizeDisplay(values?.Name);
        if (string.IsNullOrEmpty(name))
            return PersonIntakeText.Get("Lookup.Error.NameRequired");
        if (name.Length > NameMaxLength)
            return PersonIntakeText.Format("Lookup.Error.NameTooLong", NameMaxLength);
        if (values!.Name.Any(ch => char.IsControl(ch) && !char.IsWhiteSpace(ch)))
            return PersonIntakeText.Get("Lookup.Error.NameInvalid");
        if (HasReverseName(type) && CaseAddressAutocomplete.NormalizeDisplay(values.ReverseName).Length > NameMaxLength)
            return PersonIntakeText.Format("Lookup.Error.ReverseNameTooLong", NameMaxLength);
        if (HasContractFields(type) && (values.Description?.Trim().Length ?? 0) > DescriptionMaxLength)
            return PersonIntakeText.Format("Lookup.Error.DescriptionTooLong", DescriptionMaxLength);

        var key = CaseAddressAutocomplete.FoldKey(name);
        foreach (var existing in objectSpace.GetObjects(type).OfType<BaseObject>())
        {
            if (excludeId.HasValue && existing.ID == excludeId.Value)
                continue;
            if (NameKeys(existing).Any(k => string.Equals(k, key, StringComparison.Ordinal)))
                return PersonIntakeText.Format("Lookup.Error.Duplicate", name);
        }

        return null;
    }

    /// <summary>Creates (id null) or renames the record in <paramref name="objectSpace"/> and commits.</summary>
    public static Guid Save(IObjectSpace objectSpace, Type type, Guid? id, Values values)
    {
        if (!IsSupported(type))
            throw new InvalidOperationException($"{type?.Name} cannot be edited from the wizard.");
        if (id.HasValue && !CanRename(type))
            throw new InvalidOperationException("Shared catalog entries cannot be renamed here.");

        var error = Validate(objectSpace, type, id, values);
        if (error != null)
            throw new InvalidOperationException(error);

        var item = id.HasValue
            ? objectSpace.GetObjectByKey(type, id.Value)
                ?? throw new InvalidOperationException(PersonIntakeText.Get("Lookup.Error.Missing"))
            : objectSpace.CreateObject(type);

        var name = CaseAddressAutocomplete.NormalizeDisplay(values.Name);
        switch (item)
        {
            case ActualPosition actual:
                actual.Name = name;
                break;
            case LookupBase lookup:
                lookup.NameTm = name;
                break;
        }

        if (item is Relationship relationship)
        {
            var reverse = CaseAddressAutocomplete.NormalizeDisplay(values.ReverseName);
            relationship.ReverseNameTm = string.IsNullOrEmpty(reverse) ? null : reverse;
        }

        if (item is ProjectContract contract)
        {
            var description = values.Description?.Trim();
            contract.Description = string.IsNullOrEmpty(description) ? null : description;
            contract.IsActive = values.IsActive;
        }

        objectSpace.CommitChanges();
        return ((BaseObject)item).ID;
    }

    private static string[] NameKeys(BaseObject item)
    {
        switch (item)
        {
            case ActualPosition actual:
                return new[] { CaseAddressAutocomplete.FoldKey(actual.Name) };
            case LookupBase lookup:
#pragma warning disable CS0618
                return new[]
                {
                    CaseAddressAutocomplete.FoldKey(lookup.NameTm),
                    CaseAddressAutocomplete.FoldKey(lookup.Name),
                    CaseAddressAutocomplete.FoldKey(LookupLocalization.GetDisplayName(lookup))
                }.Where(k => !string.IsNullOrEmpty(k)).ToArray();
#pragma warning restore CS0618
            default:
                return Array.Empty<string>();
        }
    }
}
