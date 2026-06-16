using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProgressListLabelHelperTests
{
    [Fact]
    public void FormatStatusLabel_MinistryStep_AppendsShortName()
    {
        var label = InvokeFormatStatusLabel(
            "1st ministry review (In progress)",
            locationCode: "AT_THE_MINISTERY_1",
            "Turkmenenergo");

        Assert.Equal("1st ministry review (In progress) - Turkmenenergo", label);
    }

    [Fact]
    public void FormatStatusLabel_OfficeStep_AppendsOffice()
    {
        var label = InvokeFormatStatusLabel(
            "In preparation",
            ApplicationProgressLocationCodes.AtOffice,
            ministryShortName: null);

        Assert.Equal("In preparation - Office", label);
    }

    [Fact]
    public void FormatStatusLabel_MigrationProcessing_AppendsMigrationService()
    {
        var label = InvokeFormatStatusLabel(
            "In processing",
            ApplicationProgressLocationCodes.AtMigrationService,
            ministryShortName: string.Empty);

        Assert.Equal("In processing - Migration service", label);
    }

    [Fact]
    public void FormatStatusLabel_MigrationIssued_AppendsMigrationService()
    {
        var label = InvokeFormatStatusLabel(
            "Issued",
            ApplicationProgressLocationCodes.AtMigrationService,
            ministryShortName: null);

        Assert.Equal("Issued - Migration service", label);
    }

    [Fact]
    public void ResolveContextSuffix_MinistryTakesPrecedenceOverLocation()
    {
        var suffix = InvokeResolveContextSuffix(
            ApplicationProgressLocationCodes.AtOffice,
            "Turkmenenergo");

        Assert.Equal("Turkmenenergo", suffix);
    }

    private static string InvokeFormatStatusLabel(
        string? stateLabel,
        string? locationCode,
        string? ministryShortName)
    {
        var method = typeof(ApplicationProgressListLabelHelper).GetMethod(
            "FormatStatusLabel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, [stateLabel, locationCode, ministryShortName])!;
    }

    private static string? InvokeResolveContextSuffix(string? locationCode, string? ministryShortName)
    {
        var method = typeof(ApplicationProgressListLabelHelper).GetMethod(
            "ResolveContextSuffix",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (string?)method.Invoke(null, [locationCode, ministryShortName]);
    }
}
