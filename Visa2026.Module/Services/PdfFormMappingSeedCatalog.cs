using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Core visa-PDF mappings applied at fill time when PdfFormMapping rows are missing.
/// Family lines stay in <see cref="PdfMappingHelper.NormalizeFamilyMemberMappings"/>.
/// </summary>
internal static class PdfFormMappingSeedCatalog
{
    internal readonly record struct Seed(
        string PdfFieldKey,
        PdfMappingMode Mode,
        string? PropertyPath,
        string Description,
        string? ExpressionOrConstant = null);

    public static IReadOnlyList<Seed> Core { get; } =
    [
        new("topmostSubform[0].Page1[0].L01[0]", PdfMappingMode.Property, "Application.ApplicationType.PdfForm_Code", "Visa operation type"),
        new("topmostSubform[0].Page1[0].L02[0]", PdfMappingMode.Property, "Application.Urgency.PdfForm_Code", "Urgency (Dropdown)"),
        new("topmostSubform[0].Page2[0]._25[0]", PdfMappingMode.Property, "Application.VisaType.PdfForm_Code", "Visa Type (ApplicationProfileInstance Level)"),
        new("topmostSubform[0].Page2[0]._27[0]", PdfMappingMode.Property, "Application.VisaPeriod.PdfForm_Count", "Duration of stay (count)"),
        new("topmostSubform[0].Page2[0]._271[0]", PdfMappingMode.Property, "Application.VisaPeriod.PdfForm__Code", "Duration of stay (unit)"),
        new("topmostSubform[0].Page1[0].L10[0]", PdfMappingMode.Property, "Application.Application_Company_Name", "Company Name"),
        new("topmostSubform[0].Page1[0].L11[0]", PdfMappingMode.Property, "Application.Application_Company_Address", "Company Address"),
        new("topmostSubform[0].Page1[0].L13[0]", PdfMappingMode.Property, "Application.Application_Company_PhoneNumber", "Company Phone"),
        new("topmostSubform[0].Page1[0].L12[0]", PdfMappingMode.Property, "Application.Application_Company_Email", "Company Email"),
        new("topmostSubform[0].Page1[0].IP[1].#field[0]", PdfMappingMode.Constant, null, "Legal Entity Checkbox", "true"),
        new("topmostSubform[0].Page1[0]._01[0]", PdfMappingMode.Property, "Person.LastName", "Last Name"),
        new("topmostSubform[0].Page1[0]._03[0]", PdfMappingMode.Property, "Person.FirstName", "First Name"),
        new("topmostSubform[0].Page1[0]._04[0]", PdfMappingMode.Property, "Person.DateOfBirth", "Date of Birth"),
        new("topmostSubform[0].Page1[0]._05[0]", PdfMappingMode.Property, "Person.Gender.PdfForm_Code", "Gender"),
        new("topmostSubform[0].Page1[0]._18[0]", PdfMappingMode.Property, "Person.MaritalStatus.PdfForm_Code", "Marital Status"),
        new("topmostSubform[0].Page1[0]._08[0]", PdfMappingMode.Property, "Person.BirthPlace", "Birth Place"),
        new("topmostSubform[0].Page1[0]._06[0]", PdfMappingMode.Property, "Person.CountryOfBirth.Code", "Country of Birth"),
        new("topmostSubform[0].Page1[0]._07[0]", PdfMappingMode.Property, "Person.Nationality.Code", "Citizenship"),
        new("topmostSubform[0].Page1[0]._15[0]", PdfMappingMode.Expression, null, "Foreign Address (Country + Address)", "Concat(Person.ForeignAddressCountry.Code, ', ', Person.ForeignAddress)"),
        new("topmostSubform[0].Page1[0]._19[0]", PdfMappingMode.Property, "CurrentEducation.EducationLevel.PdfForm_Code", "Education Level"),
        new("topmostSubform[0].Page1[0]._20[0]", PdfMappingMode.Property, "CurrentEducation.Specialty.NameTm", "Specialty"),
        new("topmostSubform[0].Page1[0]._23[0]", PdfMappingMode.Property, "CurrentPositionHistory.Position.NameTm", "Work Position"),
        new("topmostSubform[0].Page1[0]._22[0]", PdfMappingMode.Expression, null, "Work Place and Phone", "Concat(Application.Application_Company_Name, ', ', Application.Application_Company_PhoneNumber)"),
        new("topmostSubform[0].Page1[0].ImageField1[0]", PdfMappingMode.Property, "Person.Photo", "Photo"),
        new("topmostSubform[0].Page1[0]._10[0]", PdfMappingMode.Property, "CurrentPassport.PassportType.PdfForm_Code", "Passport Type"),
        new("topmostSubform[0].Page1[0]._09[0]", PdfMappingMode.Property, "Person.PersonalNumber", "Personal Number"),
        new("topmostSubform[0].Page1[0]._11[0]", PdfMappingMode.Property, "CurrentPassport.PassportNumber", "Passport Number"),
        new("topmostSubform[0].Page1[0]._12[0]", PdfMappingMode.Property, "CurrentPassport.IssueDate", "Passport Issue Date"),
        new("topmostSubform[0].Page1[0]._13[0]", PdfMappingMode.Property, "CurrentPassport.ExpirationDate", "Passport Expiration Date"),
        new("topmostSubform[0].Page1[0]._14[0]", PdfMappingMode.Property, "CurrentPassport.IssuedCountry.Code", "Passport Issued Country"),
        new("topmostSubform[0].Page2[0]._26[0]", PdfMappingMode.Property, "Application.VisaCategory.PdfForm_Code", "Visa Category"),
        new("topmostSubform[0].Page2[0]._33[0]", PdfMappingMode.Property, "CurrentAddressOfResidence.Region.PdfForm_Code", "Region of stay"),
        new("topmostSubform[0].Page2[0]._34[0]", PdfMappingMode.Property, "CurrentAddressOfResidence.City.PdfForm_Code", "District of stay"),
        new("topmostSubform[0].Page2[0]._35[0]", PdfMappingMode.Property, "CurrentAddressOfResidence.FullAddress", "Stay address"),
        new("topmostSubform[0].Page2[0]._46[0]", PdfMappingMode.Property, "Pdf_AccompanyingFullName", "Accompanying person name"),
        new("topmostSubform[0].Page2[0]._45[0]", PdfMappingMode.Property, "Pdf_AccompanyingNationalityCode", "Accompanying nationality (ISO alpha-3)"),
        new("topmostSubform[0].Page2[0]._47[0]", PdfMappingMode.Property, "Pdf_AccompanyingDetail1", "Accompanying detail — relationship (Tm)"),
        new("topmostSubform[0].Page2[0]._48[0]", PdfMappingMode.Property, "Pdf_AccompanyingDetail2", "Accompanying detail — date of birth"),
        new("topmostSubform[0].Page2[0]._49[0]", PdfMappingMode.Property, "Pdf_AccompanyingDetail3", "Accompanying detail — passport no."),
        new("topmostSubform[0].Page2[0]._50[0]", PdfMappingMode.Property, "Pdf_AccompanyingDetail4", "Accompanying detail — personal ID"),
    ];
}