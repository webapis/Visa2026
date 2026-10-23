using System.Collections.Generic;
using System.Reflection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Company workbook tabs (çalışma izni, kota, Personel sayi, uzatımlar, aktiv personeller)
/// must stay on the real SQL path and map to the correct ListViews / Excel keys.
/// </summary>
public sealed class NewCompanyDashboardReportsPromotionTests
{
    [Theory]
    [InlineData(ReportDashboardCategory.WorkPermit, "calisma-izni-listesi")]
    [InlineData(ReportDashboardCategory.WorkPermit, "kota-oranimiz")]
    [InlineData(ReportDashboardCategory.WorkPermit, "personel-sayi")]
    [InlineData(ReportDashboardCategory.VisaExtension, "uzatilanlar")]
    [InlineData(ReportDashboardCategory.VisaExtension, "aktiv-personeller")]
    [InlineData(ReportDashboardCategory.Invitation, "in-process-long")]
    public void RealSubReports_IncludesNewCompanyAndLongProcessTabs(
        ReportDashboardCategory category,
        string subReport)
    {
        Assert.Contains((category, subReport), ReadRealSubReports());
    }

    [Fact]
    public void RealSubReports_UsesCatalogConstantsForNewCompanyTabs()
    {
        var promoted = ReadRealSubReports();

        Assert.Contains(
            (ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey),
            promoted);
        Assert.Contains(
            (ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitKotaOranimizKey),
            promoted);
        Assert.Contains(
            (ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitPersonelSayiKey),
            promoted);
        Assert.Contains(
            (ReportDashboardCategory.VisaExtension, ReportDashboardCatalog.VisaUzatilanlarKey),
            promoted);
        Assert.Contains(
            (ReportDashboardCategory.VisaExtension, ReportDashboardCatalog.VisaAktivPersonellerKey),
            promoted);
        Assert.Contains(
            (ReportDashboardCategory.Invitation, ReportDashboardCatalog.InvitationInProcessLongKey),
            promoted);
    }

    [Fact]
    public void ResolveListViewTarget_MapsCompanyTabsToExpectedTypes()
    {
        Assert.Equal(
            ("VwRdWorkPermitCalismaIzniListesi_ListView", typeof(VwRdWorkPermitCalismaIzniListesi)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.WorkPermit,
                ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey));
        Assert.Equal(
            ("WorkPermitQuotaSettings_ListView", typeof(WorkPermitQuotaSettings)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.WorkPermit,
                ReportDashboardCatalog.WorkPermitKotaOranimizKey));
        Assert.Equal(
            ("VwRdWorkPermitPersonelSayi_ListView", typeof(VwRdWorkPermitPersonelSayi)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.WorkPermit,
                ReportDashboardCatalog.WorkPermitPersonelSayiKey));
        Assert.Equal(
            ("VwRdRegistrationOturumSayisi_ListView", typeof(VwRdRegistrationOturumSayisi)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.Registration,
                ReportDashboardCatalog.RegistrationOturumSayisiKey));
        Assert.Equal(
            ("VwRdVisaUzatilanlar_ListView", typeof(VwRdVisaUzatilanlar)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.VisaExtension,
                ReportDashboardCatalog.VisaUzatilanlarKey));
        Assert.Equal(
            ("VwRdVisaAktivPersoneller_ListView", typeof(VwRdVisaAktivPersoneller)),
            ReportDashboardCatalog.ResolveListViewTarget(
                ReportDashboardCategory.VisaExtension,
                ReportDashboardCatalog.VisaAktivPersonellerKey));
    }

    [Fact]
    public void ExcelTemplate_Supports_NewCompanyCanonicalKeys()
    {
        Assert.Equal(
            ReportDashboardCatalog.WorkPermitKotaOranimizKey,
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.WorkPermit,
                ReportDashboardCatalog.WorkPermitKotaOranimizKey));
        Assert.Equal(
            ReportDashboardCatalog.WorkPermitPersonelSayiKey,
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.WorkPermit,
                ReportDashboardCatalog.WorkPermitPersonelSayiKey));
        Assert.Equal(
            ReportDashboardCatalog.RegistrationOturumSayisiKey,
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.Registration,
                ReportDashboardCatalog.RegistrationOturumSayisiKey));
        Assert.Equal(
            ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey,
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.WorkPermit,
                ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey));
        Assert.Equal(
            ReportDashboardCatalog.VisaUzatilanlarKey,
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.VisaExtension,
                ReportDashboardCatalog.VisaUzatilanlarKey));
        Assert.Equal(
            ReportDashboardCatalog.VisaAktivPersonellerKey,
            ReportDashboardExcelTemplateService.CanonicalKey(
                ReportDashboardCategory.VisaExtension,
                ReportDashboardCatalog.VisaAktivPersonellerKey));
    }

    [Fact]
    public void EnglishTableHeaders_IncludeAktivVizeSuresiColumn()
    {
        var headers = ReportDashboardCatalog.EnglishTableHeaders(
            ReportDashboardCategory.VisaExtension,
            ReportDashboardCatalog.VisaAktivPersonellerKey);

        Assert.Contains("Vize Süresi", headers);
        Assert.Equal(
            ReportDashboardWorkPermitCalismaExcel.AktivPersonellerHeaders.Length,
            headers.Length);
    }

    private static HashSet<(ReportDashboardCategory Category, string SubReport)> ReadRealSubReports()
    {
        var field = typeof(ReportDashboardHybridQueryService).GetField(
            "RealSubReports",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(field);

        var value = field!.GetValue(null);
        Assert.NotNull(value);
        return (HashSet<(ReportDashboardCategory, string)>)value!;
    }
}
