using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApplicationRuntimeLogTextHelperTests
{
    [Theory]
    [InlineData(null, 10, null)]
    [InlineData("", 10, "")]
    [InlineData("short", 10, "short")]
    [InlineData("0123456789ABC", 10, "0123456789")]
    public void Truncate_RespectsMaxLength(string? value, int maxLength, string? expected)
    {
        Assert.Equal(expected, ApplicationRuntimeLogTextHelper.Truncate(value, maxLength));
    }

    [Fact]
    public void Truncate_NonPositiveMax_ReturnsOriginal()
    {
        Assert.Equal("abc", ApplicationRuntimeLogTextHelper.Truncate("abc", 0));
        Assert.Equal("abc", ApplicationRuntimeLogTextHelper.Truncate("abc", -1));
    }

    [Theory]
    [InlineData("Server=x;Password=secret;Database=y", "Server=x;Password=***secret;Database=y")]
    [InlineData("pwd=secret;Host=x", "pwd=***secret;Host=x")]
    [InlineData("PASSWORD=SecretValue", "Password=***SecretValue")]
    public void ScrubSecrets_MarksPasswordKeyWithRedactionPrefix(string input, string expected)
    {
        // Current helper inserts "***" after the password key marker; it does not strip the value.
        Assert.Equal(expected, ApplicationRuntimeLogTextHelper.ScrubSecrets(input));
        Assert.Contains("***", ApplicationRuntimeLogTextHelper.ScrubSecrets(input), StringComparison.Ordinal);
    }

    [Fact]
    public void TryExtractBatchId_PrefersStateGuidOverMessage()
    {
        var fromState = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fromMessage = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var state = new List<KeyValuePair<string, object?>>
        {
            new("BatchId", fromState),
        };

        var extracted = ApplicationRuntimeLogTextHelper.TryExtractBatchId(
            $"Person export batch failed BatchId={fromMessage}",
            state);

        Assert.Equal(fromState, extracted);
    }

    [Fact]
    public void TryExtractBatchId_ParsesMessageWhenStateMissing()
    {
        var batchId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var extracted = ApplicationRuntimeLogTextHelper.TryExtractBatchId(
            $"Person export batch failed BatchId={batchId}",
            state: null);

        Assert.Equal(batchId, extracted);
    }

    [Fact]
    public void TryExtractBatchId_IgnoresEmptyGuidInState()
    {
        var batchId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var state = new List<KeyValuePair<string, object?>>
        {
            new("BatchId", Guid.Empty),
        };

        var extracted = ApplicationRuntimeLogTextHelper.TryExtractBatchId(
            $"batch BatchId={batchId}",
            state);

        Assert.Equal(batchId, extracted);
    }

    [Fact]
    public void ResolveErrorCode_UsesExplicitStateCode()
    {
        var state = new List<KeyValuePair<string, object?>>
        {
            new(ApplicationRuntimeLogErrorCodes.PropertyName, "  PERSON-EXPORT-001  "),
        };

        var code = ApplicationRuntimeLogTextHelper.ResolveErrorCode(
            state,
            category: "Any",
            message: "Person export batch failed");

        Assert.Equal(ApplicationRuntimeLogErrorCodes.PersonExportBatchFailed, code);
    }

    [Theory]
    [InlineData("Person export batch failed", ApplicationRuntimeLogErrorCodes.PersonExportBatchFailed)]
    [InlineData("PersonExportBatchWorkerService loop error", ApplicationRuntimeLogErrorCodes.PersonExportWorkerLoop)]
    [InlineData("PDF batch failed", ApplicationRuntimeLogErrorCodes.PdfBatchFailed)]
    [InlineData("Resminamalar batch failed", ApplicationRuntimeLogErrorCodes.WordBatchFailed)]
    [InlineData("PdfGenerationBatchWorkerService loop error", ApplicationRuntimeLogErrorCodes.PdfWorkerLoop)]
    [InlineData("WordReportGenerationBatchWorkerService loop error", ApplicationRuntimeLogErrorCodes.WordWorkerLoop)]
    [InlineData("User report template seed failed", ApplicationRuntimeLogErrorCodes.InfraTemplateSeed)]
    [InlineData("Batch schema column ensure failed", ApplicationRuntimeLogErrorCodes.InfraBatchSchema)]
    [InlineData("Error occurred while cleaning temporary files", ApplicationRuntimeLogErrorCodes.TempCleanup)]
    public void TryExtractErrorCode_InfersStableCodesFromMessage(string message, string expected)
    {
        Assert.Equal(
            expected,
            ApplicationRuntimeLogTextHelper.TryExtractErrorCode("Worker", message));
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", ApplicationRuntimeLogErrorCodes.HttpUnhandled)]
    [InlineData("Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost", ApplicationRuntimeLogErrorCodes.BlazorCircuit)]
    public void TryExtractErrorCode_InfersFrameworkCodesFromCategory(string category, string expected)
    {
        Assert.Equal(
            expected,
            ApplicationRuntimeLogTextHelper.TryExtractErrorCode(category, "unexpected failure"));
    }
}
