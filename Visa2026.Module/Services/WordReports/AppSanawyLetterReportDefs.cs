using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    public class AppInvSanawyReportDef : AppSanawyLetterReportDefBase
    {
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Inv" };
        public override string GetFileName(Application app) =>
            $"Sanawy_Cakylyk_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppInvAndWPSanawyReportDef : AppSanawyLetterReportDefBase
    {
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Inv_And_WP" };
        public override string GetFileName(Application app) =>
            $"Sanawy_CakylykIsRugsat_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppInvFMSanawyReportDef : AppSanawyLetterReportDefBase
    {
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Inv_FM" };
        public override string GetFileName(Application app) =>
            $"Sanawy_CakylykFM_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppCancelVisaSanawyReportDef : AppSanawyLetterReportDefBase
    {
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Cancel_Visa" };
        public override string GetFileName(Application app) =>
            $"Sanawy_WizaYatyr_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppExitVisaSanawyReportDef : AppSanawyLetterReportDefBase
    {
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Exit_Visa" };
        public override string GetFileName(Application app) =>
            $"Sanawy_CykysWizasy_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppAdditionalWPLocationSanawyReportDef : AppSanawyLetterReportDefBase
    {
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Additional_WP_location" };
        public override string GetFileName(Application app) =>
            $"Sanawy_IsRugsatnamaYer_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppVisaAndWPExtSanawyReportDef : AppSanawyLetterReportDefBase
    {
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Visa_and_WP_Ext" };
        public override string GetFileName(Application app) =>
            $"Sanawy_WizaIsUzatmak_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppVisaExtFMSanawyReportDef : AppSanawyLetterReportDefBase
    {
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Visa_Ext_FM" };
        public override string GetFileName(Application app) =>
            $"Sanawy_WizaUzatmaFM_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }

    public class AppBorderZonePermissionSanawyReportDef : AppSanawyLetterReportDefBase
    {
        public override string[] ApplicableApplicationTypeNames => new[] { "App_Border_Zone_Permission" };
        public override string GetFileName(Application app) =>
            $"Sanawy_SerhetYaka_{app.FullApplicationNumber}_{DateTime.Now:yyyyMMdd}.docx";
    }
}
