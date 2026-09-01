using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>
/// Live tenant Company / Signatory / Representative for the profile wizard.
/// Values are read from Configuration singletons — never copied onto <see cref="ApplicationProfile"/>.
/// </summary>
public sealed class ApplicationProfileWizardOrganizationSnapshot
{
    public static ApplicationProfileWizardOrganizationSnapshot Empty { get; } = new();

    public bool HasCompany { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string CompanyPhone { get; init; } = string.Empty;
    public string CompanyAddress { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string CompanyTaxInformation { get; init; } = string.Empty;
    public string CompanyRegistrationDateText { get; init; } = string.Empty;

    public bool HasSignatory { get; init; }
    public string SignatoryFullName { get; init; } = string.Empty;
    public string SignatoryPositionTitleTm { get; init; } = string.Empty;
    public string SignatoryPassportNumber { get; init; } = string.Empty;
    public string SignatoryPassportAuthority { get; init; } = string.Empty;
    public string SignatoryPassportIssueDateText { get; init; } = string.Empty;

    public bool HasRepresentative { get; init; }
    public string RepresentativeFullName { get; init; } = string.Empty;
    public string RepresentativePositionTitleTm { get; init; } = string.Empty;
    public string RepresentativePhone { get; init; } = string.Empty;
    public string RepresentativePassportNumber { get; init; } = string.Empty;
    public string RepresentativePassportAuthority { get; init; } = string.Empty;
    public string RepresentativePassportIssueDateText { get; init; } = string.Empty;

    public static ApplicationProfileWizardOrganizationSnapshot Load(IObjectSpace? objectSpace)
    {
        if (objectSpace == null)
            return Empty;

        var company = CompanyProfile.TryGetInstance(objectSpace);
        var signatory = AuthorizedSignatory.TryGetInstance(objectSpace);
        var representative = AuthorizedRepresentative.TryGetInstance(objectSpace);

        return new ApplicationProfileWizardOrganizationSnapshot
        {
            HasCompany = company != null,
            CompanyName = company?.Name ?? string.Empty,
            CompanyCode = company?.Code ?? string.Empty,
            CompanyPhone = company?.PhoneNumber ?? string.Empty,
            CompanyAddress = company?.Address ?? string.Empty,
            CompanyEmail = company?.Email ?? string.Empty,
            CompanyTaxInformation = company?.TaxInformation ?? string.Empty,
            CompanyRegistrationDateText = company?.RegistrationDateText ?? string.Empty,

            HasSignatory = signatory != null,
            SignatoryFullName = signatory?.FullName ?? string.Empty,
            SignatoryPositionTitleTm = signatory?.PositionTitleTm ?? string.Empty,
            SignatoryPassportNumber = signatory?.PassportNumber ?? string.Empty,
            SignatoryPassportAuthority = signatory?.PassportAuthority ?? string.Empty,
            SignatoryPassportIssueDateText = FormatDate(signatory?.PassportIssueDate),

            HasRepresentative = representative != null,
            RepresentativeFullName = representative?.FullName ?? string.Empty,
            RepresentativePositionTitleTm = representative?.PositionTitleTm ?? string.Empty,
            RepresentativePhone = representative?.Phone ?? string.Empty,
            RepresentativePassportNumber = representative?.PassportNumber ?? string.Empty,
            RepresentativePassportAuthority = representative?.PassportAuthority ?? string.Empty,
            RepresentativePassportIssueDateText = FormatDate(representative?.PassportIssueDate),
        };
    }

    private static string FormatDate(DateTime? value) =>
        value is { } date && date != default ? date.ToString("dd.MM.yyyy") : string.Empty;
}