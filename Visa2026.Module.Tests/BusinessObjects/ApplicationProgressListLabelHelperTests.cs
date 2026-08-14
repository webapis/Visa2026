using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileInstanceProgressListLabelHelperTests
{
    [Fact]
    public void FormatStatusLabel_MinistryStep_AppendsShortName()
    {
        var label = InvokeFormatStatusLabel(
            "Ylalaşykdan çykdy",
            "Turkmenenergo");

        Assert.Equal("Ylalaşykdan çykdy - Turkmenenergo", label);
    }

    [Fact]
    public void FormatStatusLabel_WithoutMinistry_ReturnsStateOnly()
    {
        var label = InvokeFormatStatusLabel(
            "Ofisde",
            ministryShortName: null);

        Assert.Equal("Ofisde", label);
    }

    [Fact]
    public void FormatStatusLabel_EmptyMinistry_ReturnsStateOnly()
    {
        var label = InvokeFormatStatusLabel(
            "Işlenilýär",
            ministryShortName: string.Empty);

        Assert.Equal("Işlenilýär", label);
    }

    private static string InvokeFormatStatusLabel(string? stateLabel, string? ministryShortName)
    {
        var method = typeof(ApplicationProfileInstanceProgressListLabelHelper).GetMethod(
            "FormatStatusLabel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method.Invoke(null, [stateLabel, ministryShortName])!;
    }
}
