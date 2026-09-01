#nullable enable

using System.Text.Json;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanAmbiguousYellowAzurePayloadTests
{
    [Fact]
    public void Build_includes_role_description_and_snippet_not_file_bytes()
    {
        var set = new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile { RequirePersonPassport = true },
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

        var payload = ScanAmbiguousYellowAzurePayload.Build(new ScanAmbiguousYellowRefinementRequest
        {
            Playbook = new ScanAuthoringPlaybook { Markdown = "rules", Fingerprint = "fp", VersionLabel = "1" },
            PlaceholderSet = set,
            SourceKind = ScanSourceKind.Word,
            Marks =
            [
                new ScanAmbiguousYellowMark
                {
                    FieldId = "n1",
                    YellowText = "Nepesowa Tumar Aşyrowna",
                    PrintedLabel = "Wekil ady",
                    SurroundingSnippet = "Wekil ady: <<<Nepesowa Tumar Aşyrowna>>>",
                    LocalProposedToken = "{{ds.RPFN}}",
                },
            ],
        });

        var json = JsonSerializer.Serialize(payload);
        Assert.Contains("\"role\":\"Wekil\"", json, StringComparison.Ordinal);
        Assert.Contains("\"role\":\"Applicant\"", json, StringComparison.Ordinal);
        Assert.Contains("\"relatedBo\":\"Passport\"", json, StringComparison.Ordinal);
        Assert.Contains("\"relatedBo\":\"AuthorizedRepresentative\"", json, StringComparison.Ordinal);
        Assert.Contains("allowedTokensByBo", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"allowedTokens\":", json, StringComparison.Ordinal);
        Assert.Contains("never a visa applicant", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("roster / applicant person", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nepesowa Tumar", json, StringComparison.Ordinal);
        Assert.Contains("surroundingSnippet", json, StringComparison.Ordinal);
        Assert.Contains("\\u003C\\u003C\\u003C", json, StringComparison.Ordinal);
        Assert.Contains("\"printedLabel\":\"Wekil ady\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("OfficePackage", json, StringComparison.OrdinalIgnoreCase);
    }
}
