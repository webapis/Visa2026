using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.PersonLinkedDocuments;

/// <summary>
/// Maps Application Profile <c>RequirePerson*</c> toggles to Person document-copies
/// section ids and ApplicationItem document-copies family keys.
/// </summary>
public static class PersonDocumentCopiesProfileSectionFilter
{
    public const string SectionPassports = "Passports";
    /// <summary>People submenu preview mode: passport beside each linked visa scan.</summary>
    public const string SectionPassportsWithVisas = "PassportsWithVisas";
    public const string SectionEducation = "Education";
    public const string SectionPositions = "Positions";
    public const string SectionAddresses = "Addresses";
    public const string SectionInvitations = "Invitations";
    public const string SectionWorkPermits = "WorkPermits";
    public const string SectionMedicalRecords = "MedicalRecords";
    public const string SectionRejections = "Rejections";

    public const string FamilyPassport = "Passport";
    public const string FamilyEducation = "Education";
    public const string FamilyAddress = "AddressOfResidence";
    public const string FamilyVisa = "Visa";
    public const string FamilyInvitation = "Invitation";
    public const string FamilyWorkPermit = "WorkPermit";
    public const string FamilyMedical = "MedicalRecord";
    public const string FamilyRejection = "Rejection";

    /// <summary>
    /// Person catalog section ids allowed by the profile (Passports covers Passport and/or Visa).
    /// </summary>
    public static IReadOnlyList<string> GetAllowedPersonSectionIds(ApplicationProfile? profile)
    {
        if (profile == null)
            return Array.Empty<string>();

        var ids = new List<string>();
        if (profile.RequirePersonPassport || profile.RequirePersonVisa)
            ids.Add(SectionPassports);
        if (profile.RequirePersonEducation
            && ApplicationProfileEducationPolicy.AllowsPersonEducation(profile))
            ids.Add(SectionEducation);
        if (profile.RequirePersonPosition)
            ids.Add(SectionPositions);
        if (profile.RequirePersonAddressOfResidence)
            ids.Add(SectionAddresses);
        if (profile.RequirePersonInvitationItem)
            ids.Add(SectionInvitations);
        if (profile.RequirePersonWorkPermitItem)
            ids.Add(SectionWorkPermits);
        if (profile.RequirePersonMedical)
            ids.Add(SectionMedicalRecords);
        if (profile.RequirePersonRejectionItem)
            ids.Add(SectionRejections);
        return ids;
    }

    public static bool IsPersonSectionAllowed(ApplicationProfile? profile, string? sectionId)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
            return false;
        var catalogId = NormalizeCatalogSectionId(sectionId);
        return GetAllowedPersonSectionIds(profile)
            .Contains(catalogId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Maps People menu keys (e.g. PassportsWithVisas) to the catalog section id.</summary>
    public static string NormalizeCatalogSectionId(string? sectionId)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
            return string.Empty;
        var id = sectionId.Trim();
        if (string.Equals(id, SectionPassportsWithVisas, StringComparison.OrdinalIgnoreCase))
            return SectionPassports;
        return id;
    }

    /// <summary>People Passports &amp; visas preview pairs each linked visa scan beside its passport.</summary>
    public static bool IncludesNestedVisas(string? sectionId) =>
        string.Equals(sectionId?.Trim(), SectionPassportsWithVisas, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// People Preview document copies menu rows: Passports expands to passport-only + Passports &amp; visas.
    /// </summary>
    public static IReadOnlyList<string> ExpandPeopleMenuSectionIds(IReadOnlyList<string> allowedCatalogSectionIds)
    {
        if (allowedCatalogSectionIds == null || allowedCatalogSectionIds.Count == 0)
            return Array.Empty<string>();

        var result = new List<string>();
        foreach (var id in allowedCatalogSectionIds)
        {
            if (string.Equals(id, SectionPassports, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(SectionPassports);
                result.Add(SectionPassportsWithVisas);
                continue;
            }

            result.Add(id);
        }

        return result;
    }

    /// <summary>
    /// ApplicationItem By-type family keys allowed by the profile (Passport and Visa are separate).
    /// Border zone / salary / travel / family-relationship are excluded per product lock.
    /// </summary>
    public static IReadOnlyList<string> GetAllowedApplicationItemFamilyKeys(ApplicationProfile? profile)
    {
        if (profile == null)
            return Array.Empty<string>();

        var ids = new List<string>();
        if (profile.RequirePersonPassport)
            ids.Add(FamilyPassport);
        if (profile.RequirePersonEducation
            && ApplicationProfileEducationPolicy.AllowsPersonEducation(profile))
            ids.Add(FamilyEducation);
        if (profile.RequirePersonAddressOfResidence)
            ids.Add(FamilyAddress);
        if (profile.RequirePersonVisa)
            ids.Add(FamilyVisa);
        if (profile.RequirePersonInvitationItem)
            ids.Add(FamilyInvitation);
        if (profile.RequirePersonWorkPermitItem)
            ids.Add(FamilyWorkPermit);
        if (profile.RequirePersonMedical)
            ids.Add(FamilyMedical);
        if (profile.RequirePersonRejectionItem)
            ids.Add(FamilyRejection);
        return ids;
    }

    public static bool IsApplicationItemFamilyAllowed(ApplicationProfile? profile, string? familyKey)
    {
        if (string.IsNullOrWhiteSpace(familyKey))
            return false;
        return GetAllowedApplicationItemFamilyKeys(profile)
            .Contains(familyKey.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static string PersonSectionLabel(string sectionId, string? cultureName = null) =>
        sectionId switch
        {
            SectionPassports => VisaUiMessages.Get("PersonDocumentCopies.Section.Passports", cultureName),
            SectionPassportsWithVisas => VisaUiMessages.Get("PersonDocumentCopies.Section.PassportsWithVisas", cultureName),
            SectionEducation => VisaUiMessages.Get("PersonDocumentCopies.Section.Education", cultureName),
            SectionPositions => VisaUiMessages.Get("PersonDocumentCopies.Section.Positions", cultureName),
            SectionAddresses => VisaUiMessages.Get("PersonDocumentCopies.Section.Addresses", cultureName),
            SectionInvitations => VisaUiMessages.Get("PersonDocumentCopies.Section.Invitations", cultureName),
            SectionWorkPermits => VisaUiMessages.Get("PersonDocumentCopies.Section.WorkPermits", cultureName),
            SectionMedicalRecords => VisaUiMessages.Get("PersonDocumentCopies.Section.MedicalRecords", cultureName),
            SectionRejections => VisaUiMessages.Get("PersonDocumentCopies.Section.Rejections", cultureName),
            _ => sectionId,
        };

    public static ApplicationProfileInstancePersonLinkKind? TryMapPersonSectionToLinkKind(string? sectionId) =>
        sectionId?.Trim() switch
        {
            SectionPassports => ApplicationProfileInstancePersonLinkKind.Passport,
            SectionPassportsWithVisas => ApplicationProfileInstancePersonLinkKind.Passport,
            SectionEducation => ApplicationProfileInstancePersonLinkKind.Education,
            SectionPositions => ApplicationProfileInstancePersonLinkKind.Position,
            SectionAddresses => ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            SectionInvitations => ApplicationProfileInstancePersonLinkKind.InvitationItem,
            SectionWorkPermits => ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
            SectionMedicalRecords => ApplicationProfileInstancePersonLinkKind.MedicalRecord,
            SectionRejections => ApplicationProfileInstancePersonLinkKind.RejectionItem,
            _ => null,
        };

    public static ApplicationProfileInstancePersonLinkKind? TryMapApplicationItemFamilyToLinkKind(string? familyKey) =>
        familyKey?.Trim() switch
        {
            FamilyPassport => ApplicationProfileInstancePersonLinkKind.Passport,
            FamilyVisa => ApplicationProfileInstancePersonLinkKind.Visa,
            FamilyEducation => ApplicationProfileInstancePersonLinkKind.Education,
            FamilyAddress => ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            FamilyInvitation => ApplicationProfileInstancePersonLinkKind.InvitationItem,
            FamilyWorkPermit => ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
            FamilyMedical => ApplicationProfileInstancePersonLinkKind.MedicalRecord,
            FamilyRejection => ApplicationProfileInstancePersonLinkKind.RejectionItem,
            _ => null,
        };
}