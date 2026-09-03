#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Letterhead values for Word/PDF merge. Reads the instance's live Company / Signatory / Representative FKs.
/// </summary>
public static class ApplicationProfileInstanceOrganizationLetterheadHelper
{
    public static ApplicationProfileInstanceOrganizationLetterhead Resolve(
        ApplicationProfileInstance? application,
        IObjectSpace? objectSpace = null)
    {
        var os = objectSpace ?? ObjectSpaceHelper.Get(application);
        var company = application?.OrganizationCompany ?? OrganizationCatalogHelper.TryGetDefaultCompany(os);
        var signatory = application?.OrganizationSignatory ?? OrganizationCatalogHelper.TryGetDefaultSignatory(os);
        var representative = application?.OrganizationRepresentative
            ?? OrganizationCatalogHelper.TryGetDefaultRepresentative(os);
        var assigned = application?.OrganizationCompany != null
            || application?.OrganizationSignatory != null
            || application?.OrganizationRepresentative != null;

        return FromParts(company, signatory, representative, assigned, os);
    }

    public static void CopyFromConfigurationIfEmpty(
        ApplicationProfileInstance application,
        IObjectSpace? objectSpace = null) =>
        OrganizationCatalogHelper.AssignDefaultsIfEmpty(application, objectSpace);

    public static void ResetFromConfiguration(
        ApplicationProfileInstance application,
        IObjectSpace? objectSpace = null)
    {
        ArgumentNullException.ThrowIfNull(application);
        var os = objectSpace ?? ObjectSpaceHelper.Get(application);
        if (os == null)
            return;

        application.OrganizationCompany = OrganizationCatalogHelper.TryGetDefaultCompany(os);
        application.OrganizationSignatory = OrganizationCatalogHelper.TryGetDefaultSignatory(os);
        application.OrganizationRepresentative = OrganizationCatalogHelper.TryGetDefaultRepresentative(os);
    }

    public static int BackfillUncopied(IObjectSpace objectSpace, bool apply = true)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (!apply)
        {
            return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                .Count(a => a.OrganizationCompany == null
                    && a.OrganizationSignatory == null
                    && a.OrganizationRepresentative == null);
        }

        return OrganizationCatalogHelper.BackfillUnassigned(objectSpace);
    }

    public static ApplicationProfileInstanceOrganizationLetterhead FromInstance(ApplicationProfileInstance application)
    {
        ArgumentNullException.ThrowIfNull(application);
        return FromParts(
            application.OrganizationCompany,
            application.OrganizationSignatory,
            application.OrganizationRepresentative,
            assigned: true,
            ObjectSpaceHelper.Get(application));
    }

    public static ApplicationProfileInstanceOrganizationLetterhead FromConfiguration(IObjectSpace? objectSpace) =>
        FromParts(
            OrganizationCatalogHelper.TryGetDefaultCompany(objectSpace),
            OrganizationCatalogHelper.TryGetDefaultSignatory(objectSpace),
            OrganizationCatalogHelper.TryGetDefaultRepresentative(objectSpace),
            assigned: false,
            objectSpace);

    private static ApplicationProfileInstanceOrganizationLetterhead FromParts(
        CompanyProfile? company,
        AuthorizedSignatory? signatory,
        AuthorizedRepresentative? representative,
        bool assigned,
        IObjectSpace? objectSpace) =>
        new()
        {
            Copied = assigned,
            CompanyId = company?.ID,
            SignatoryId = signatory?.ID,
            RepresentativeId = representative?.ID,
            CompanyName = company?.Name,
            CompanyCode = company?.Code,
            CompanyPhone = company?.PhoneNumber,
            CompanyAddress = company?.Address,
            CompanyEmail = company?.Email,
            CompanyTaxInformation = company?.TaxInformation,
            CompanyRegistrationDate = company?.RegistrationDate,
            SignatoryFullName = signatory?.FullName,
            SignatoryPositionTitleTm = signatory?.PositionTitleTm,
            SignatoryPassportNumber = signatory?.PassportNumber,
            SignatoryPassportAuthority = signatory?.PassportAuthority,
            SignatoryPassportIssueDate = signatory?.PassportIssueDate,
            SignatoryPassportExpirationDate = signatory?.PassportExpirationDate,
            RepresentativeFullName = representative?.FullName,
            RepresentativePositionTitleTm = representative?.PositionTitleTm,
            RepresentativePhone = representative?.Phone,
            RepresentativePassportNumber = representative?.PassportNumber,
            RepresentativePassportAuthority = representative?.PassportAuthority,
            RepresentativePassportIssueDate = representative?.PassportIssueDate,
            CompanyOptions = OrganizationCatalogHelper.ListCompanies(objectSpace),
            SignatoryOptions = OrganizationCatalogHelper.ListSignatories(objectSpace),
            RepresentativeOptions = OrganizationCatalogHelper.ListRepresentatives(objectSpace),
        };
}

public sealed class ApplicationProfileInstanceOrganizationLetterhead
{
    public bool Copied { get; init; }
    public Guid? CompanyId { get; init; }
    public Guid? SignatoryId { get; init; }
    public Guid? RepresentativeId { get; init; }
    public string? CompanyName { get; init; }
    public string? CompanyCode { get; init; }
    public string? CompanyPhone { get; init; }
    public string? CompanyAddress { get; init; }
    public string? CompanyEmail { get; init; }
    public string? CompanyTaxInformation { get; init; }
    public DateTime? CompanyRegistrationDate { get; init; }
    public string? SignatoryFullName { get; init; }
    public string? SignatoryPositionTitleTm { get; init; }
    public string? SignatoryPassportNumber { get; init; }
    public string? SignatoryPassportAuthority { get; init; }
    public DateTime? SignatoryPassportIssueDate { get; init; }
    public DateTime? SignatoryPassportExpirationDate { get; init; }
    public string? RepresentativeFullName { get; init; }
    public string? RepresentativePositionTitleTm { get; init; }
    public string? RepresentativePhone { get; init; }
    public string? RepresentativePassportNumber { get; init; }
    public string? RepresentativePassportAuthority { get; init; }
    public DateTime? RepresentativePassportIssueDate { get; init; }
    public IReadOnlyList<OrganizationCatalogOption> CompanyOptions { get; init; } = Array.Empty<OrganizationCatalogOption>();
    public IReadOnlyList<OrganizationCatalogOption> SignatoryOptions { get; init; } = Array.Empty<OrganizationCatalogOption>();
    public IReadOnlyList<OrganizationCatalogOption> RepresentativeOptions { get; init; } = Array.Empty<OrganizationCatalogOption>();

    public string CompanyRegistrationDateText =>
        OrganizationPassportLineHelper.FormatIssueDateText(CompanyRegistrationDate);

    public string SignatoryPassportIssueDateText =>
        OrganizationPassportLineHelper.FormatIssueDateText(SignatoryPassportIssueDate);

    public string SignatoryPassportExpirationDateText =>
        OrganizationPassportLineHelper.FormatIssueDateText(SignatoryPassportExpirationDate);

    public string SignatoryPassportLine =>
        OrganizationPassportLineHelper.Format(SignatoryPassportNumber, SignatoryPassportAuthority, SignatoryPassportIssueDate);

    public string RepresentativePassportIssueDateText =>
        OrganizationPassportLineHelper.FormatIssueDateText(RepresentativePassportIssueDate);

    public string RepresentativePassportLine =>
        OrganizationPassportLineHelper.Format(
            RepresentativePassportNumber,
            RepresentativePassportAuthority,
            RepresentativePassportIssueDate);

    public string CompanyRegistryAddressLine
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(CompanyTaxInformation))
                parts.Add(CompanyTaxInformation.Trim());
            if (!string.IsNullOrWhiteSpace(CompanyAddress))
                parts.Add(CompanyAddress.Trim());
            if (!string.IsNullOrWhiteSpace(CompanyPhone))
                parts.Add(CompanyPhone.Trim());
            return string.Join(" ", parts);
        }
    }

    public string RepresentativePassportPhoneLine =>
        OrganizationPassportLineHelper.FormatNumberAuthorityPhone(
            RepresentativePassportNumber,
            RepresentativePassportAuthority,
            RepresentativePhone);

    public static string Display(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
}