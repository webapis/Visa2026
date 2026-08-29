#nullable enable

using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanGapPacketExporterTests
{
    private readonly ScanGapPacketExporter _exporter = new();

    [Fact]
    public void ComputeContentSha256_is_stable_hex()
    {
        var hash = ScanGapPacketExporter.ComputeContentSha256([1, 2, 3, 4]);
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, ScanGapPacketExporter.ComputeContentSha256([1, 2, 3, 4]));
    }

    [Fact]
    public void ExportJson_includes_gaps_and_context()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile { ID = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee") },
                DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var plan = new ScanFieldPlanMerger().Merge(new ScanFieldPlanMergeRequest
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.BlankForm,
            Proposal = new ScanFieldPlanProposal
            {
                Fields =
                [
                    new ScanDetectedFieldDraft
                    {
                        FieldId = "f-gap",
                        PageIndex = 0,
                        LabelText = "Employer tax id",
                        ProposedToken = null,
                        Confidence = ScanFieldConfidence.Low,
                        Scope = ScanFieldScope.Header,
                        Box = ScanBoundingBox.FullPage,
                    },
                ],
                Gaps =
                [
                    new ScanGapDraft("g1", "Employer tax id", "EmployerTaxId"),
                ],
                Source = "test",
            },
        });

        var bytes = _exporter.ExportJson(new ScanGapPacketRequest
        {
            ApplicationProfileId = set.ApplicationProfileId,
            ScanContentSha256 = "abc123",
            FieldPlan = plan,
            Validation = null,
            PlaybookFingerprint = "playbook-fp",
            PlaceholderSetFingerprint = set.Fingerprint,
            TemplateName = "Border form",
            ScanFileName = "scan.png",
            ProfileName = "Work permit",
        });

        using var doc = JsonDocument.Parse(bytes);
        Assert.Equal("1", doc.RootElement.GetProperty("schemaVersion").GetString());
        Assert.Equal("Border form", doc.RootElement.GetProperty("templateName").GetString());
        Assert.True(doc.RootElement.GetProperty("gaps").GetArrayLength() >= 1);
        Assert.Contains(
            doc.RootElement.GetProperty("gaps").EnumerateArray(),
            e => e.GetProperty("labelText").GetString() == "Employer tax id");
    }

    [Fact]
    public void ExportMarkdown_lists_gaps_and_validation()
    {
        var set = new ApplicationProfilePlaceholderSet
        {
            ApplicationProfileId = Guid.NewGuid(),
            DataScope = ApplicationProfileTemplateDataScope.ApplicationHeader,
            TemplateKind = ApplicationProfileTemplateKind.Word,
            Allowed = Array.Empty<UserReportPlaceholderCatalogEntry>(),
            Excluded = Array.Empty<PlaceholderExclusion>(),
            Fingerprint = "set-fp",
        };

        var plan = new ScanFieldPlan
        {
            PlaceholderSet = set,
            ScanKind = ScanKind.BlankForm,
            Fields = Array.Empty<ScanDetectedField>(),
            StaticRegions = Array.Empty<ScanStaticRegion>(),
            Gaps = [new ScanGap("g1", "Custom field", "CustomField")],
            PendingQuestions = Array.Empty<ScanClarificationPrompt>(),
            Source = "test",
        };

        var markdown = System.Text.Encoding.UTF8.GetString(_exporter.ExportMarkdown(new ScanGapPacketRequest
        {
            ApplicationProfileId = set.ApplicationProfileId,
            ScanContentSha256 = "deadbeef",
            FieldPlan = plan,
            Validation = new TemplateValidationReport(
                Array.Empty<string>(),
                Array.Empty<PlaceholderValidationResult>(),
                [new TemplateValidationIssue("Unknown token", TemplateValidationSeverity.Error, TemplateValidationIssueCode.UnknownToken, "{{ds.MISSING}}")],
                true),
            PlaybookFingerprint = "playbook-fp",
            PlaceholderSetFingerprint = set.Fingerprint,
        }));

        Assert.Contains("# Template scan gap packet", markdown);
        Assert.Contains("Custom field", markdown);
        Assert.Contains("Unknown token", markdown);
    }

    [Fact]
    public void DI_registers_gap_exporter()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddTemplateScan();
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IScanGapPacketExporter>());
    }
}
