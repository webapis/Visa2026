#nullable enable

using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ScanCompanyRegistrationDateGuardTests
{
    private static ApplicationProfilePlaceholderSet Set() =>
        new ApplicationProfilePlaceholderSetService(new UserReportPlaceholderCatalogService()).GetSet(
            new ApplicationProfilePlaceholderSetQuery
            {
                Profile = new ApplicationProfile(),
                DataScope = ApplicationProfileTemplateDataScope.Both,
                TemplateKind = ApplicationProfileTemplateKind.Word,
            });

    [Fact]
    public void Rewrites_adat_to_acrdt_when_nearby_is_company_registration()
    {
        var set = Set();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ADAT" };
        var rewritten = ScanCompanyRegistrationDateGuard.RewriteDraft(
            new ScanDetectedFieldDraft
            {
                FieldId = "2",
                PageIndex = 0,
                LabelText = "02.02.2009ý.",
                ProposedToken = "{{ds.ADAT}}",
                Confidence = ScanFieldConfidence.High,
                Scope = ScanFieldScope.Header,
                Box = ScanBoundingBox.FullPage,
                NearbyLabel = "hasaba alyş belgesi № 12345 02.02.2009ý. Aşgabat",
            },
            set,
            used);

        Assert.Equal("{{ds.ACRDT}}", rewritten.ProposedToken);
        Assert.DoesNotContain("ADAT", used);
        Assert.Contains("ACRDT", used);
    }

    [Fact]
    public void Keeps_adat_for_letter_header_number_and_date()
    {
        var set = Set();
        var rewritten = ScanCompanyRegistrationDateGuard.RewriteDraft(
            new ScanDetectedFieldDraft
            {
                FieldId = "h",
                PageIndex = 0,
                LabelText = "28.04.2026 ý.",
                ProposedToken = "{{ds.ADAT}}",
                Confidence = ScanFieldConfidence.High,
                Scope = ScanFieldScope.Header,
                Box = ScanBoundingBox.FullPage,
                NearbyLabel = "№ 4/-434 28.04.2026 ý.",
            },
            set);

        Assert.Equal("{{ds.ADAT}}", rewritten.ProposedToken);
    }
}