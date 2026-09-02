using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Shared data shaping for Word and Excel user report merge.</summary>
public static class UserReportMergeDataHelper
{
    public static IList<ApplicationRosterMergeLine> GetActiveApplicationItems(ApplicationProfileInstance application) =>
        ApplicationRosterHelper.GetMergeLineItems(application);

    /// <summary>
    /// Loads roster lines for merge from skip-navigation People + ResolvedLinks
    /// into <see cref="ApplicationRosterMergeLine"/> projections.
    /// </summary>
    public static IList<ApplicationRosterMergeLine> GetActiveApplicationItems(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (application == null)
            throw new ArgumentNullException(nameof(application));

        return ApplicationRosterHelper.GetMergeLineItems(objectSpace, application);
    }

    public static Dictionary<string, object> BuildApplicationHeaderDictionary(ApplicationProfileInstance application)
    {
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["FullApplicationNumber"] = application.FullApplicationNumber ?? string.Empty,
            ["ApplicationDateText"] = application.ApplicationDateText ?? string.Empty,
            ["CompanyName"] = application.Application_Company_Name ?? string.Empty,
            ["Application_Company_RegistrationDateText"] = application.Application_Company_RegistrationDateText ?? string.Empty,
        };
        UserReportPlaceholderAliasRegistry.EnrichDictionary(data);
        return data;
    }

    /// <summary>Row keys aligned with <c>Sanaw_ckl.docx</c> / <c>Sanaw_uzt.docx</c> (14-column sanawy).</summary>
    public static Dictionary<string, object> BuildSanawyRowDictionary(ApplicationRosterMergeLine item, int rowNo) =>
        WithAliasKeys(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNo"] = rowNo,
            ["Person_LastName"] = item.Person_LastName ?? string.Empty,
            ["Person_FirstName"] = item.Person_FirstName ?? string.Empty,
            ["Person_MiddleName"] = item.Person_MiddleName ?? string.Empty,
            ["Person_DateOfBirthText"] = item.Person_DateOfBirthText ?? string.Empty,
            ["Person_CountryOfBirthTm"] = item.Person_CountryOfBirthTm ?? string.Empty,
            ["Person_BirthPlace"] = item.Person_BirthPlace ?? string.Empty,
            ["Person_GenderTm"] = item.Person_GenderTm ?? string.Empty,
            ["Person_MaritalStatusTm"] = item.Person_MaritalStatusTm ?? string.Empty,
            ["Person_NationalityCode"] = item.Person_NationalityCode ?? string.Empty,
            ["Person_NationalityTm"] = item.Person_NationalityTm ?? string.Empty,
            ["Passport_Number"] = item.Passport_Number ?? string.Empty,
            ["Passport_TypeTm"] = item.Passport_TypeTm ?? string.Empty,
            ["Passport_Authority"] = item.Passport_Authority ?? string.Empty,
            ["Passport_CountryCode"] = item.Passport_CountryCode ?? string.Empty,
            ["Passport_CountryTm"] = item.Passport_CountryTm ?? string.Empty,
            ["Passport_ExpirationDateText"] = item.Passport_ExpirationDateText ?? string.Empty,
            ["Education_LevelTm"] = item.Education_LevelTm ?? string.Empty,
            ["Education_InstitutionName"] = item.Education_InstitutionName ?? string.Empty,
            ["Education_CountryCode"] = item.Education_CountryCode ?? string.Empty,
            ["Education_GraduationYear"] = item.Education_GraduationYear ?? string.Empty,
            ["Education_SpecialtyTm"] = item.Education_SpecialtyTm ?? string.Empty,
            ["Position_PositionTm"] = item.Position_PositionTm ?? string.Empty,
            ["Application_VisaPeriod_NameTm"] = item.Application_VisaPeriod_NameTm ?? string.Empty,
            ["Application_VisaCategory_NameTm"] = item.Application_VisaCategory_NameTm ?? string.Empty,
            ["Address_FullAddress"] = item.Address_FullAddress ?? string.Empty,
            ["Person_ForeignAddress"] = item.Person_ForeignAddress ?? string.Empty,
            ["Person_ForeignAddressCountryCode"] = item.Person_ForeignAddressCountryCode ?? string.Empty,
            ["Person_PreviousWorkplacesInTurkmenistan"] = item.Person_PreviousWorkplacesInTurkmenistan ?? string.Empty,
            ["Person_VisaApplicationFamilyMembersText"] = item.Person_VisaApplicationFamilyMembersText ?? string.Empty,
            ["Application_BorderZoneLocation_NameTm"] = item.Application_BorderZoneLocation_NameTm ?? string.Empty,
            ["Item_BorderZoneLocation_NameTm"] = item.Item_BorderZoneLocation_NameTm ?? string.Empty,
            ["BorderZoneLocation_NameTm"] = item.BorderZoneLocation_NameTm ?? string.Empty,
        });

    public static List<Dictionary<string, object>> BuildSanawyStyleRows(
        ApplicationProfileInstance application,
        IList<ApplicationRosterMergeLine>? applicationItems = null)
    {
        var items = applicationItems != null && applicationItems.Count > 0
            ? applicationItems.Where(i => i != null).ToList()
            : GetActiveApplicationItems(application);
        var rows = new List<Dictionary<string, object>>(items.Count);
        for (int i = 0; i < items.Count; i++)
            rows.Add(BuildSanawyRowDictionary(items[i], i + 1));
        return rows;
    }

    /// <summary>True when template row tokens match <c>Forma_16.docx</c> / registration certificate.</summary>
    public static bool TemplateUsesRegistrationForm16RowPlaceholders(
        UserReportTemplate? template,
        IEnumerable<UserReportPlaceholder>? placeholders) =>
        IsForma16UserReportTemplate(template)
        || (placeholders != null && placeholders.Any(p =>
            p.IsValid
            && RowTokenReferences(p.PlaceholderKey, "Registration_GelmeginMaksadyTm")))
        || (placeholders != null
            && placeholders.Any(p => p.IsValid && RowTokenReferences(p.PlaceholderKey, "Visa_IssueDateText"))
            && placeholders.Any(p => p.IsValid && RowTokenReferences(p.PlaceholderKey, "Person_FullName")));

    public static bool IsForma16UserReportTemplate(UserReportTemplate? template) =>
        template != null
        && (string.Equals(template.TemplateName, "Forma 16", StringComparison.OrdinalIgnoreCase)
            || (template.TemplateFile?.FileName?.Contains("Forma_16", StringComparison.OrdinalIgnoreCase) ?? false));

    public static bool IsSahsyKagyzUserReportTemplate(UserReportTemplate? template) =>
        template != null
        && (LooksLikeSahsyKagyzName(template.TemplateName)
            || LooksLikeSahsyKagyzName(template.TemplateFile?.FileName));

    /// <summary>
    /// Seed <c>Sahsy kagyz</c> / <c>sahsy_kagyz.docx</c> and officer copies such as
    /// <c>SAHSY KAGYZ_117</c>.
    /// </summary>
    public static bool LooksLikeSahsyKagyzName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var folded = name.Replace('_', ' ').Replace('-', ' ');
        return folded.Contains("sahsy", StringComparison.OrdinalIgnoreCase)
            && folded.Contains("kagyz", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TemplateUsesSahsyKagyzRowPlaceholders(
        UserReportTemplate? template,
        IEnumerable<UserReportPlaceholder>? placeholders) =>
        IsSahsyKagyzUserReportTemplate(template)
        || (placeholders != null && placeholders.Any(p =>
            p.IsValid
            && (RowTokenReferences(p.PlaceholderKey, "SahsyKagyz_FamilyStatusText")
                || RowTokenReferences(p.PlaceholderKey, "Person_VisaApplicationFamilyMembersText")
                || RowTokenReferences(p.PlaceholderKey, "Education_CountryCode"))));

    public static bool IsWizaYatyrylmakSanawUserReportTemplate(UserReportTemplate? template) =>
        template != null
        && (string.Equals(template.TemplateName, "Wiza ýatyrmak sanaw", StringComparison.OrdinalIgnoreCase)
            || (template.TemplateFile?.FileName?.Contains("wiza_yatyrylmak_sanaw", StringComparison.OrdinalIgnoreCase) ?? false));

    public static bool TemplateUsesWizaYatyrylmakSanawRowPlaceholders(
        UserReportTemplate? template,
        IEnumerable<UserReportPlaceholder>? placeholders) =>
        IsWizaYatyrylmakSanawUserReportTemplate(template)
        || (placeholders != null && placeholders.Any(p =>
            p.IsValid
            && (RowTokenReferences(p.PlaceholderKey, "CancelVisa_NumberBlock")
                || (p.PlaceholderKey.StartsWith(".", StringComparison.Ordinal)
                    && p.PlaceholderKey.Contains("CancelVisa_NumberBlock", StringComparison.OrdinalIgnoreCase)))));

    private static bool RowTokenReferences(string placeholderKey, string propertyName) =>
        !string.IsNullOrEmpty(placeholderKey)
        && (placeholderKey.Contains($"rows.{propertyName}", StringComparison.OrdinalIgnoreCase)
            || (placeholderKey.StartsWith(".", StringComparison.Ordinal)
                && placeholderKey.Contains(propertyName, StringComparison.OrdinalIgnoreCase)));

    public static List<Dictionary<string, object>> BuildRegistrationForm16StyleRows(
        ApplicationProfileInstance application,
        IList<ApplicationRosterMergeLine>? applicationItems = null)
    {
        var items = applicationItems != null && applicationItems.Count > 0
            ? applicationItems.Where(i => i != null).ToList()
            : GetActiveApplicationItems(application);
        var rows = new List<Dictionary<string, object>>(items.Count);
        for (int i = 0; i < items.Count; i++)
            rows.Add(BuildRegistrationForm16RowDictionary(items[i], i + 1));
        return rows;
    }

    public static List<Dictionary<string, object>> BuildSahsyKagyzStyleRows(
        ApplicationProfileInstance application,
        IList<ApplicationRosterMergeLine>? applicationItems = null)
    {
        var items = applicationItems != null && applicationItems.Count > 0
            ? applicationItems.Where(i => i != null).ToList()
            : GetActiveApplicationItems(application);
        var rows = new List<Dictionary<string, object>>(items.Count);
        for (int i = 0; i < items.Count; i++)
            rows.Add(BuildSahsyKagyzRowDictionary(items[i], i + 1));
        return rows;
    }

    public static List<Dictionary<string, object>> BuildWizaYatyrylmakSanawStyleRows(
        ApplicationProfileInstance application,
        IList<ApplicationRosterMergeLine>? applicationItems = null)
    {
        var items = applicationItems != null && applicationItems.Count > 0
            ? applicationItems.Where(i => i != null).ToList()
            : GetActiveApplicationItems(application);
        var rows = new List<Dictionary<string, object>>(items.Count);
        for (int i = 0; i < items.Count; i++)
            rows.Add(BuildWizaYatyrylmakSanawRowDictionary(items[i], i + 1));
        return rows;
    }

    /// <summary>Row keys for <c>Excel/wiza_yatyrylmak_sanaw.xlsx</c> (and Word if used): App_Cancel_Visa list; stacked CurrentVisa + NextVisa per row.</summary>
    public static Dictionary<string, object> BuildWizaYatyrylmakSanawExcelRowDictionary(ApplicationRosterMergeLine item, int rowNumber)
    {
        var row = new Dictionary<string, object>(BuildWizaYatyrylmakSanawRowDictionary(item, rowNumber), StringComparer.OrdinalIgnoreCase)
        {
            ["RowNumber"] = rowNumber,
        };
        return WithAliasKeys(row);
    }

    /// <summary>Row keys for <c>wiza_yatyrylmak_sanaw</c> merge (Word <c>{{ds.rows.*}}</c> or Excel <c>{{.*}}</c>).</summary>
    public static Dictionary<string, object> BuildWizaYatyrylmakSanawRowDictionary(ApplicationRosterMergeLine item, int rowNo) =>
        WithAliasKeys(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNo"] = rowNo,
            ["Person_LastName"] = item.Person_LastName ?? string.Empty,
            ["Person_FirstName"] = item.Person_FirstName ?? string.Empty,
            ["Person_DateOfBirthText"] = item.Person_DateOfBirthText ?? string.Empty,
            ["Person_GenderTm"] = item.Person_GenderTm ?? string.Empty,
            ["Person_NationalityCode"] = item.Person_NationalityCode ?? string.Empty,
            ["Passport_Number"] = item.Passport_Number ?? string.Empty,
            ["Passport_TypeTm"] = item.Passport_TypeTm ?? string.Empty,
            ["Passport_Authority"] = item.Passport_Authority ?? string.Empty,
            ["Passport_CountryCode"] = item.Passport_CountryCode ?? string.Empty,
            ["Passport_CountryTm"] = item.Passport_CountryTm ?? string.Empty,
            ["Passport_ExpirationDateText"] = item.Passport_ExpirationDateText ?? string.Empty,
            ["Registration_GelmeginMaksadyTm"] = item.Registration_GelmeginMaksadyTm ?? string.Empty,
            ["CancelVisa_NumberBlock"] = item.CancelVisa_NumberBlock ?? string.Empty,
            ["CancelVisa_StartDateBlock"] = item.CancelVisa_StartDateBlock ?? string.Empty,
            ["CancelVisa_ExpirationDateBlock"] = item.CancelVisa_ExpirationDateBlock ?? string.Empty,
        });

    /// <summary>Row keys for <c>sahsy_kagyz.docx</c> (ŞAHSY KAGYZY, ItemRows + photo).</summary>
    public static Dictionary<string, object> BuildSahsyKagyzRowDictionary(ApplicationRosterMergeLine item, int rowNumber) =>
        WithAliasKeys(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNumber"] = rowNumber,
            ["Person_FullName"] = item.Person_FullName ?? string.Empty,
            ["Person_DateOfBirthText"] = item.Person_DateOfBirthText ?? string.Empty,
            ["Person_CountryOfBirthCode"] = item.Person_CountryOfBirthCode ?? string.Empty,
            ["Person_BirthPlace"] = item.Person_BirthPlace ?? string.Empty,
            ["Person_NationalityCode"] = item.Person_NationalityCode ?? string.Empty,
            ["Passport_Number"] = item.Passport_Number ?? string.Empty,
            ["Passport_TypeTm"] = item.Passport_TypeTm ?? string.Empty,
            ["Passport_Authority"] = item.Passport_Authority ?? string.Empty,
            ["Passport_CountryCode"] = item.Passport_CountryCode ?? string.Empty,
            ["Passport_CountryTm"] = item.Passport_CountryTm ?? string.Empty,
            ["Passport_IssueDateText"] = item.Passport_IssueDateText ?? string.Empty,
            ["Passport_ExpirationDateText"] = item.Passport_ExpirationDateText ?? string.Empty,
            ["Passport_PersonalNumber"] = item.Passport_PersonalNumber ?? string.Empty,
            ["Education_LevelTm"] = item.Education_LevelTm ?? string.Empty,
            ["Education_CountryCode"] = item.Education_CountryCode ?? string.Empty,
            ["Education_InstitutionName"] = item.Education_InstitutionName ?? string.Empty,
            ["Education_GraduationYear"] = item.Education_GraduationYear ?? string.Empty,
            ["Education_SpecialtyTm"] = item.Education_SpecialtyTm ?? string.Empty,
            ["Position_PositionTm"] = item.Position_PositionTm ?? string.Empty,
            ["Person_PreviousWorkplacesInTurkmenistan"] = item.Person_PreviousWorkplacesInTurkmenistan ?? string.Empty,
            ["Person_VisaApplicationFamilyMembersText"] = item.Person_VisaApplicationFamilyMembersText ?? string.Empty,
            ["SahsyKagyz_FamilyStatusText"] = item.SahsyKagyz_FamilyStatusText ?? string.Empty,
            ["Person_ForeignAddressWithCountry"] = item.Person_ForeignAddressWithCountry ?? string.Empty,
            ["Application_SponsorName"] = item.Application_SponsorName ?? string.Empty,
            ["Application_CompanyHead_PositionTm"] = item.Application_CompanyHead_PositionTm ?? string.Empty,
            ["Application_CompanyHead_FullName"] = item.Application_CompanyHead_FullName ?? string.Empty,
            ["Person_Photo"] = item.Person_Photo ?? Array.Empty<byte>(),
        });

    /// <summary>Row keys for <c>Forma_16.docx</c> (registration certificate, ItemRows).</summary>
    public static Dictionary<string, object> BuildRegistrationForm16RowDictionary(ApplicationRosterMergeLine item, int rowNumber) =>
        WithAliasKeys(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNumber"] = rowNumber,
            ["Person_FullName"] = item.Person_FullName ?? string.Empty,
            ["Person_NationalityCode"] = item.Person_NationalityCode ?? string.Empty,
            ["Person_DateOfBirthText"] = item.Person_DateOfBirthText ?? string.Empty,
            ["Passport_Number"] = item.Passport_Number ?? string.Empty,
            ["Passport_TypeTm"] = item.Passport_TypeTm ?? string.Empty,
            ["Passport_Authority"] = item.Passport_Authority ?? string.Empty,
            ["Passport_CountryCode"] = item.Passport_CountryCode ?? string.Empty,
            ["Passport_CountryTm"] = item.Passport_CountryTm ?? string.Empty,
            ["Passport_ExpirationDateText"] = item.Passport_ExpirationDateText ?? string.Empty,
            ["Passport_IssueDateText"] = item.Passport_IssueDateText ?? string.Empty,
            ["Person_CountryOfBirthCode"] = item.Person_CountryOfBirthCode ?? string.Empty,
            ["Person_CountryOfBirthTm"] = item.Person_CountryOfBirthTm ?? string.Empty,
            ["Person_BirthPlace"] = item.Person_BirthPlace ?? string.Empty,
            ["Person_GenderTm"] = item.Person_GenderTm ?? string.Empty,
            ["Person_MiddleName"] = item.Person_MiddleName ?? string.Empty,
            ["Person_MaritalStatusTm"] = item.Person_MaritalStatusTm ?? string.Empty,
            ["Person_NationalityTm"] = item.Person_NationalityTm ?? string.Empty,
            ["Person_ForeignAddressCountryCode"] = item.Person_ForeignAddressCountryCode ?? string.Empty,
            ["Person_ForeignAddress"] = item.Person_ForeignAddress ?? string.Empty,
            ["Travel_PurposeOfTravelTm"] = item.Travel_PurposeOfTravelTm ?? string.Empty,
            ["Registration_GelmeginMaksadyTm"] = item.Registration_GelmeginMaksadyTm ?? string.Empty,
            ["Person_IsEmployee"] = item.Person_IsEmployee,
            ["Person_RelationshipTm"] = item.Person_RelationshipTm ?? string.Empty,
            ["Person_SponsoringEmployeeFullName"] = item.Person_SponsoringEmployeeFullName ?? string.Empty,
            ["Person_SponsoringEmployeePositionTm"] = item.Person_SponsoringEmployeePositionTm ?? string.Empty,
            ["Address_FullAddress"] = item.Address_FullAddress ?? string.Empty,
            ["Visa_CategoryTm"] = item.Visa_CategoryTm ?? string.Empty,
            ["Visa_TypeTm"] = item.Visa_TypeTm ?? string.Empty,
            ["Visa_Number"] = item.Visa_Number ?? string.Empty,
            ["Visa_IssuedPlaceTm"] = item.Visa_IssuedPlaceTm ?? string.Empty,
            ["Visa_IssueDateText"] = item.Visa_IssueDateText ?? string.Empty,
            ["Visa_StartDateText"] = item.Visa_StartDateText ?? string.Empty,
            ["Visa_ExpirationDateText"] = item.Visa_ExpirationDateText ?? string.Empty,
            ["Travel_DateText"] = item.Travel_DateText ?? string.Empty,
            ["Travel_CheckPointTm"] = item.Travel_CheckPointTm ?? string.Empty,
            ["Application_SponsorName"] = item.Application_SponsorName ?? string.Empty,
            ["Application_CompanyAddress"] = item.Application_CompanyAddress ?? string.Empty,
            ["Application_MigrationServiceCode"] = item.Application_MigrationServiceCode ?? string.Empty,
            ["Application_RegistrationDateText"] = item.Application_RegistrationDateText ?? string.Empty,
            ["Application_DateText"] = item.Application_DateText ?? string.Empty,
            ["Application_FullNumber"] = item.Application_FullNumber ?? string.Empty,
            ["Person_Photo"] = item.Person_Photo ?? Array.Empty<byte>(),
        });

    /// <summary>True when template row tokens use sanawy / ministry list shape (<c>Person_LastName</c>, <c>RowNo</c>).</summary>
    public static bool TemplateUsesPersonListRowPlaceholders(IEnumerable<UserReportPlaceholder>? placeholders) =>
        placeholders != null
        && placeholders.Any(p =>
            p.IsValid
            && p.PlaceholderKey.Contains("rows.", StringComparison.OrdinalIgnoreCase)
            && (p.PlaceholderKey.Contains("Person_LastName", StringComparison.OrdinalIgnoreCase)
                || p.PlaceholderKey.Contains("RowNo", StringComparison.OrdinalIgnoreCase)));

    /// <summary>Word sanawy list seeds (<c>Sanaw_uzt.docx</c>, <c>Sanaw_ckl.docx</c>).</summary>
    public static bool IsSanawUserReportTemplate(UserReportTemplate? template)
    {
        if (template == null)
            return false;

        if (string.Equals(template.TemplateName, "Sanaw", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.TemplateName, "Sanaw_ckl", StringComparison.OrdinalIgnoreCase)
            || template.TemplateName.StartsWith("Sanaw", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var fileName = template.TemplateFile?.FileName;
        if (string.IsNullOrEmpty(fileName)
            || fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return fileName.Contains("Sanaw", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ministry sanawy Word lists (<c>Sanaw_uzt.docx</c>, <c>Sanaw_ckl.docx</c>): one document with all selected
    /// application lines in <c>{{#ds.rows}}</c>, not one file per person (unlike Contract).
    /// </summary>
    public static bool UsesSingleDocumentItemList(UserReportTemplate? template) =>
        template != null
        && template.GetEffectiveOutputFormat() == TemplateOutputFormat.Word
        && ShouldUseSanawyStyleRows(template, template.Placeholders);

    /// <summary>Row list for a single-line ItemRows template (Contract, Forma 16, sahsy_kagyz, etc.).</summary>
    public static List<Dictionary<string, object>> BuildSingleItemRowsForTemplate(
        ApplicationRosterMergeLine item,
        UserReportTemplate template,
        int rowNo = 1)
    {
        if (ShouldUseSanawyStyleRows(template, template.Placeholders))
            return [BuildSanawyRowDictionary(item, rowNo)];

        if (TemplateUsesRegistrationForm16RowPlaceholders(template, template.Placeholders)
            || IsForma16UserReportTemplate(template))
            return [BuildRegistrationForm16RowDictionary(item, rowNo)];

        if (TemplateUsesSahsyKagyzRowPlaceholders(template, template.Placeholders)
            || IsSahsyKagyzUserReportTemplate(template))
            return [BuildSahsyKagyzRowDictionary(item, rowNo)];

        if (TemplateUsesWizaYatyrylmakSanawRowPlaceholders(template, template.Placeholders)
            || IsWizaYatyrylmakSanawUserReportTemplate(template))
            return [BuildWizaYatyrylmakSanawRowDictionary(item, rowNo)];

        return
        [
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Person_FullName"] = item.Person_FullName ?? string.Empty,
                ["Person_DateOfBirthText"] = item.Person_DateOfBirthText ?? string.Empty,
                ["Position_PositionTm"] = item.Position_PositionTm ?? string.Empty,
                ["Passport_Number"] = item.Passport_Number ?? string.Empty,
                ["Passport_TypeTm"] = item.Passport_TypeTm ?? string.Empty,
                ["Passport_Authority"] = item.Passport_Authority ?? string.Empty,
                ["Passport_CountryCode"] = item.Passport_CountryCode ?? string.Empty,
                ["Passport_CountryTm"] = item.Passport_CountryTm ?? string.Empty,
                ["Application_SponsorName"] = item.Application_SponsorName ?? string.Empty,
                ["Application_SponsorSignatory"] = item.Application_SponsorSignatory ?? string.Empty,
                ["Application_CompanyAddress"] = item.Application_CompanyAddress ?? string.Empty,
                ["Application_CompanyRegistryAddressLine"] = item.Application_CompanyRegistryAddressLine ?? string.Empty,
                ["CompanyHead_FullName"] = item.CompanyHead_FullName ?? string.Empty,
                ["CompanyHead_PassportLine"] = item.CompanyHead_PassportLine ?? string.Empty,
                ["Representative_FullName"] = item.Representative_FullName ?? string.Empty,
                ["Representative_PassportLine"] = item.Representative_PassportLine ?? string.Empty,
                ["Contract_StartDateText"] = item.Contract_StartDateText ?? string.Empty,
                ["Contract_ExpirationDateText"] = item.Contract_ExpirationDateText ?? string.Empty,
                ["Contract_PeriodFallbackText"] = item.Contract_PeriodFallbackText ?? string.Empty,
                ["Contract_SalaryText"] = item.Contract_SalaryText ?? string.Empty,
                ["Salary_CurrencyCode"] = item.Salary_CurrencyCode ?? string.Empty,
            }
        ];
    }

    /// <summary>
    /// True when merge must use <see cref="BuildSanawyStyleRows"/> (not labor-contract rows).
    /// </summary>
    public static bool ShouldUseSanawyStyleRows(
        UserReportTemplate? template,
        IEnumerable<UserReportPlaceholder>? placeholders,
        IEnumerable<string>? scannedTokens = null)
    {
        if (TemplateUsesRegistrationForm16RowPlaceholders(template, placeholders)
            || TemplateUsesSahsyKagyzRowPlaceholders(template, placeholders)
            || TemplateUsesWizaYatyrylmakSanawRowPlaceholders(template, placeholders))
        {
            return false;
        }

        if (IsSanawUserReportTemplate(template)
            || TemplateUsesPersonListRowPlaceholders(placeholders))
        {
            return true;
        }

        return scannedTokens != null && ScannedTokensIndicateSanawyRows(scannedTokens);
    }

    public static bool ScannedTokensIndicateSanawyRows(IEnumerable<string> tokens) =>
        tokens.Any(ScannedTokenIndicatesSanawyRow);

    private static bool ScannedTokenIndicatesSanawyRow(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var token = raw.TrimStart('#').TrimStart('/').Trim();
        if (token.StartsWith("ds.", StringComparison.OrdinalIgnoreCase) && token.Length > 3)
            token = token.Substring(3);

        return token.Contains("rows.Person_LastName", StringComparison.OrdinalIgnoreCase)
            || token.Contains("rows.RowNo", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when template row tokens use <c>{{.Person_LastName}}</c> / list columns (Excel ministry seeds).</summary>
    public static bool TemplateUsesDotRowPlaceholders(IEnumerable<string> placeholders) =>
        placeholders.Any(p =>
            p.StartsWith(".", StringComparison.Ordinal)
            && (p.Contains("Person_LastName", StringComparison.OrdinalIgnoreCase)
                || p.Contains("RowNumber", StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Row keys for Excel list templates (<c>433_gurlusyk_uzt.xlsx</c>, <c>433-ek_uzt.xlsx</c>) and Word sanawy lists.
    /// Supports both <c>{{.Property}}</c> and <c>{{ds.rows.Property}}</c>.
    /// </summary>
    public static Dictionary<string, object> BuildExcelItemListRowDictionary(ApplicationRosterMergeLine item, int rowNumber)
    {
        var row = new Dictionary<string, object>(BuildSanawyRowDictionary(item, rowNumber), StringComparer.OrdinalIgnoreCase)
        {
            ["RowNumber"] = rowNumber,
            ["Education_LevelAndInstitutionTm"] = item.Education_LevelAndInstitutionTm ?? string.Empty,
            ["Visa_DurationFrequencyBlock"] = item.Visa_DurationFrequencyBlock ?? string.Empty,
            ["WorkDuty_Description"] = item.WorkDuty_Description ?? string.Empty,
            ["Application_SponsorName"] = item.Application_SponsorName ?? string.Empty,
            ["Application_DateText"] = item.Application_DateText ?? string.Empty,
            ["Application_FullNumber"] = item.Application_FullNumber ?? string.Empty,
            ["Person_ForeignAddressWithCountry"] = item.Person_ForeignAddressWithCountry ?? string.Empty,
            ["Person_ForeignAddressCountryCode"] = item.Person_ForeignAddressCountryCode ?? string.Empty,
            ["Visa_Number"] = item.Visa_Number ?? string.Empty,
            ["Visa_IssueDateText"] = item.Visa_IssueDateText ?? string.Empty,
            ["Visa_StartDateText"] = item.Visa_StartDateText ?? string.Empty,
            ["Visa_ExpirationDateText"] = item.Visa_ExpirationDateText ?? string.Empty,
            ["Visa_CategoryTm"] = item.Visa_CategoryTm ?? string.Empty,
            ["Visa_TypeTm"] = item.Visa_TypeTm ?? string.Empty,
            ["Registration_GelmeginMaksadyTm"] = item.Registration_GelmeginMaksadyTm ?? string.Empty,
            ["WorkPermit_WorkPermittedLocations"] = item.WorkPermit_WorkPermittedLocations ?? string.Empty,
        };

        return WithAliasKeys(row);
    }

    public static Dictionary<string, object> WithAliasKeys(Dictionary<string, object> row)
    {
        UserReportPlaceholderAliasRegistry.EnrichDictionary(row);
        return row;
    }

    /// <summary>Row keys aligned with labor-contract Word templates and Contract.docx.</summary>
    public static Dictionary<string, object> BuildItemRowDictionary(ApplicationRosterMergeLine item, int rowNumber) =>
        WithAliasKeys(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNumber"] = rowNumber,
            ["Person_FullName"] = item.Person_FullName ?? string.Empty,
            ["Position_PositionTm"] = item.Position_PositionTm ?? string.Empty,
            ["Passport_Number"] = item.Passport_Number ?? string.Empty,
            ["Passport_TypeTm"] = item.Passport_TypeTm ?? string.Empty,
            ["Passport_Authority"] = item.Passport_Authority ?? string.Empty,
            ["Passport_CountryCode"] = item.Passport_CountryCode ?? string.Empty,
            ["Passport_CountryTm"] = item.Passport_CountryTm ?? string.Empty,
            ["Visa_Number"] = item.Visa_Number ?? string.Empty,
            ["Visa_ExpirationDateText"] = item.Visa_ExpirationDateText ?? string.Empty,
            ["WorkPermit_Number"] = item.WorkPermit_Number ?? string.Empty,
            ["WorkPermit_ExpirationDateText"] = item.WorkPermit_ExpirationDateText ?? string.Empty,
            ["Application_SponsorName"] = item.Application_SponsorName ?? string.Empty,
            ["Application_SponsorSignatory"] = item.Application_SponsorSignatory ?? string.Empty,
            ["Application_CompanyAddress"] = item.Application_CompanyAddress ?? string.Empty,
            ["Contract_StartDateText"] = item.Contract_StartDateText ?? string.Empty,
            ["Contract_ExpirationDateText"] = item.Contract_ExpirationDateText ?? string.Empty,
            ["Contract_PeriodFallbackText"] = item.Contract_PeriodFallbackText ?? string.Empty,
            ["Contract_SalaryText"] = item.Contract_SalaryText ?? string.Empty,
            ["Salary_CurrencyCode"] = item.Salary_CurrencyCode ?? string.Empty,
        });

    /// <summary>
    /// Letter templates from yellow marks often emit <c>{{.PFN}}</c> without <c>{{#ds.rows}}</c>.
    /// DocxTemplater then looks up those keys on <c>ds</c> and merge returns nothing.
    /// Copy first-roster (or instance) values onto the root model when no row loop is present.
    /// </summary>
    public static void PromoteLooseRowTokensOntoRoot(
        IReadOnlyList<string> extractedTokens,
        IDictionary<string, object> data,
        object? rootObject,
        IList<ApplicationRosterMergeLine>? applicationItems)
    {
        ArgumentNullException.ThrowIfNull(extractedTokens);
        ArgumentNullException.ThrowIfNull(data);

        if (HasRowOrItemLoop(extractedTokens))
            return;

        var firstRow = ResolveFirstRowSource(rootObject, applicationItems);
        if (firstRow == null && rootObject == null)
            return;

        foreach (var raw in extractedTokens)
        {
            if (string.IsNullOrWhiteSpace(raw) || !raw.StartsWith('.'))
                continue;

            var key = StripDocxModelPrefix(raw.TrimStart('.'));
            if (key.Length == 0 || key.Contains('.'))
                continue;

            var canonical = UserReportPlaceholderAliasRegistry.ResolveCanonicalPropertyPath(key);
            var value = GetPropertyValue(firstRow, canonical)
                ?? GetPropertyValue(rootObject, canonical);
            var coerced = UserReportPlaceholderBindingHelper.CoerceMergeValue(value, canonical);
            data[key] = coerced;
            if (!string.Equals(key, canonical, StringComparison.OrdinalIgnoreCase))
                data[canonical] = coerced;
        }

        UserReportPlaceholderAliasRegistry.EnrichDictionary(data);
    }

    private static bool HasRowOrItemLoop(IReadOnlyList<string> extractedTokens)
    {
        foreach (var raw in extractedTokens)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var name = StripDocxModelPrefix(raw.TrimStart('#', '/'));
            if (name.Equals("rows", StringComparison.OrdinalIgnoreCase)
                || name.Equals("ApplicationItems", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static object? ResolveFirstRowSource(
        object? rootObject,
        IList<ApplicationRosterMergeLine>? applicationItems)
    {
        if (applicationItems is { Count: > 0 })
            return applicationItems[0];
        if (rootObject is ApplicationRosterMergeLine line)
            return line;
        if (rootObject is ApplicationProfileInstance instance)
        {
            var items = GetActiveApplicationItems(instance);
            return items.Count > 0 ? items[0] : instance;
        }

        return rootObject;
    }

    public static string StripDocxModelPrefix(string pathFromTemplate)
    {
        if (string.IsNullOrWhiteSpace(pathFromTemplate))
            return pathFromTemplate ?? string.Empty;

        var p = pathFromTemplate.Trim();
        if (p.StartsWith("ds.", StringComparison.OrdinalIgnoreCase) && p.Length > 3)
            return p.Substring(3);

        return p;
    }

    public static object? GetPropertyValue(object? obj, string propertyPath)
    {
        if (obj == null || string.IsNullOrEmpty(propertyPath))
            return null;

        var path = UserReportPlaceholderAliasRegistry.ResolveCanonicalPropertyPath(propertyPath);
        var parts = path.Split('.');
        object? current = obj;

        foreach (var part in parts)
        {
            if (current == null)
                return null;

            var property = current.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
                return null;

            current = property.GetValue(current);
        }

        return current;
    }
}
