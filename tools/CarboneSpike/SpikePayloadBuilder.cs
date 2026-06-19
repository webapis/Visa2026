using System.Text.Json;
using System.Text.Json.Serialization;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Tools.CarboneSpike;

/// <summary>Builds <c>{ "d": … }</c> JSON for Carbone Studio and legacy <c>ds</c> bind dictionaries.</summary>
internal static class SpikePayloadBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static Dictionary<string, object> BuildDsPayload(SpikeScenario scenario, int itemCount, bool sampleRows = false)
    {
        var application = SpikeSampleFactory.BuildApplication(itemCount, withVisaSample: true);
        var items = UserReportMergeDataHelper.GetActiveApplicationItems(application);

        var header = UserReportMergeDataHelper.BuildApplicationHeaderDictionary(application);
        header["Application_CompanyHead_FullName"] = "Saparow A.";
        header["Application_CompanyHead_PositionTm"] = "Müdiri";
        header["CompanyHead_FullName"] = "Saparow A.";
        header["CompanyHead_PositionTm"] = "Müdiri";

        if (sampleRows)
        {
            header["CompanyName"] = "Çalyk Enerji Türkmenistandaky şahamçasy";
            header["Application_SponsorName"] = "Çalyk Enerji Türkmenistandaky şahamçasy";
            header["Application_CompanyAddress"] = "Aşgabat ş., Bitarap Türkmenistan şaýoly 538";
        }

        var rows = scenario switch
        {
            SpikeScenario.GurlusykExcel => BuildGurlusykRows(items),
            SpikeScenario.SanawWord => UserReportMergeDataHelper.BuildSanawyStyleRows(application, items),
            SpikeScenario.Forma16Word => UserReportMergeDataHelper.BuildRegistrationForm16StyleRows(application, items),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        if (sampleRows)
            ApplySampleRowOverlay(scenario, rows);

        header["rows"] = rows;
        return header;
    }

    public static string BuildCarboneJson(SpikeScenario scenario, int itemCount, bool sampleRows = false, bool wrapInD = false)
    {
        var payload = BuildDsPayload(scenario, itemCount, sampleRows);
        if (wrapInD)
        {
            var root = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { ["d"] = payload };
            return JsonSerializer.Serialize(root, JsonOptions);
        }

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static List<Dictionary<string, object>> BuildGurlusykRows(IList<ApplicationItem> items)
    {
        var rows = new List<Dictionary<string, object>>(items.Count);
        for (int i = 0; i < items.Count; i++)
            rows.Add(UserReportMergeDataHelper.BuildExcelItemListRowDictionary(items[i], i + 1));
        return rows;
    }

    private static void ApplySampleRowOverlay(SpikeScenario scenario, List<Dictionary<string, object>> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            foreach (var pair in BuildSampleRowOverlay(scenario, i + 1))
                rows[i][pair.Key] = pair.Value;
        }
    }

    private static Dictionary<string, object> BuildSampleRowOverlay(SpikeScenario scenario, int index) =>
        scenario switch
        {
            SpikeScenario.GurlusykExcel => BuildGurlusykSampleRow(index),
            SpikeScenario.SanawWord => BuildSanawSampleRow(index),
            SpikeScenario.Forma16Word => BuildForma16SampleRow(index),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

    private static Dictionary<string, object> BuildGurlusykSampleRow(int i) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNumber"] = i,
            ["RowNo"] = i,
            ["Person_LastName"] = $"Familiýa{i}",
            ["Person_FirstName"] = $"Ady{i}",
            ["Person_DateOfBirthText"] = $"{i:00}.01.1990",
            ["Person_GenderTm"] = i % 2 == 0 ? "erkek" : "aýal",
            ["Person_NationalityCode"] = "TKM",
            ["Passport_Number"] = $"P{i:000000}",
            ["Passport_ExpirationDateText"] = "01.01.2030",
            ["Education_LevelAndInstitutionTm"] = "Orta — MYU",
            ["Education_SpecialtyTm"] = "Informatika",
            ["Position_PositionTm"] = "Inžener",
            ["Visa_DurationFrequencyBlock"] = $"6 aý, köp gezeklik (A{i:0000000})",
            ["Visa_StartDateText"] = "19.02.2026",
            ["Visa_ExpirationDateText"] = "06.08.2026",
            ["Visa_Number"] = $"A{i:0000000}",
            ["Visa_CategoryTm"] = "köp gezeklik",
            ["Address_FullAddress"] = "Aşgabat, köçe 1",
            ["Person_ForeignAddressWithCountry"] = "DEU, Berlin",
            ["WorkPermit_WorkPermittedLocations"] = "Aşgabat, Mary",
            ["Application_BorderZoneLocation_NameTm"] = "Daşoguz",
            ["WorkDuty_Description"] = "Gurluşyk işleri",
            ["Application_SponsorName"] = "Çalyk Enerji Türkmenistandaky şahamçasy",
            ["Application_DateText"] = "24.03.2026",
            ["Application_FullNumber"] = "3/-433",
        };

    private static Dictionary<string, object> BuildSanawSampleRow(int i) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNo"] = i,
            ["Person_LastName"] = $"Familiýa{i}",
            ["Person_FirstName"] = $"Ady{i}",
            ["Person_DateOfBirthText"] = $"{i:00}.01.1990",
            ["Person_CountryOfBirthTm"] = "Türkmenistan",
            ["Person_BirthPlace"] = "Aşgabat",
            ["Person_GenderTm"] = "erkek",
            ["Person_NationalityCode"] = "TKM",
            ["Passport_Number"] = $"P{i:000000}",
            ["Passport_ExpirationDateText"] = "01.01.2030",
            ["Education_LevelTm"] = "Orta",
            ["Education_InstitutionName"] = "MYU",
            ["Education_SpecialtyTm"] = "Informatika",
            ["Position_PositionTm"] = "Inžener",
            ["Application_VisaPeriod_NameTm"] = "6 aý",
            ["Application_VisaCategory_NameTm"] = "köp gezeklik",
            ["Address_FullAddress"] = "Aşgabat",
            ["Person_ForeignAddress"] = "Berlin",
            ["Person_ForeignAddressCountryCode"] = "DEU",
            ["Application_BorderZoneLocation_NameTm"] = "Daşoguz",
        };

    private static Dictionary<string, object> BuildForma16SampleRow(int i) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["RowNumber"] = i,
            ["Person_FullName"] = $"Familiýa{i} Ady{i}",
            ["Person_NationalityCode"] = "TUR",
            ["Person_DateOfBirthText"] = "18.01.1977",
            ["Passport_Number"] = $"U{i:0000000}",
            ["Passport_ExpirationDateText"] = "20.05.2034",
            ["Passport_IssueDateText"] = "20.05.2024",
            ["Person_CountryOfBirthCode"] = "TUR",
            ["Person_BirthPlace"] = "Türkiye/Gaziantep",
            ["Person_GenderTm"] = "Aýal",
            ["Person_ForeignAddressCountryCode"] = "TUR",
            ["Person_ForeignAddress"] = "Emek mahallesi, Gaziantep",
            ["Registration_GelmeginMaksadyTm"] = "Türkmenistandaky şahamça müdiriniň orunbasary",
            ["Address_FullAddress"] = "Aşgabat ş., köçe 1, öý 86",
            ["Visa_CategoryTm"] = "FM",
            ["Visa_TypeTm"] = "köp gezeklik",
            ["Visa_Number"] = $"A{i:0000000}",
            ["Visa_IssuedPlaceTm"] = "Aşgabat şäher howa menzilindäki MGP",
            ["Visa_IssueDateText"] = "20.01.2026",
            ["Visa_StartDateText"] = "20.01.2026",
            ["Visa_ExpirationDateText"] = "06.07.2026 çenli",
            ["Travel_DateText"] = "20.01.2026",
            ["Travel_CheckPointTm"] = "Aşgabat şäher howa menzilindäki MGP",
            ["Application_SponsorName"] = "Çalyk Enerji Türkmenistandaky şahamçasy",
            ["Application_CompanyAddress"] = "Aşgabat ş., Bitarap Türkmenistan şaýoly 538",
            ["Application_MigrationServiceCode"] = "TDMGAS",
            ["Application_RegistrationDateText"] = "20.01.2026",
            ["Application_DateText"] = "20.01.2026",
            ["Application_FullNumber"] = "1/-120",
        };
}
