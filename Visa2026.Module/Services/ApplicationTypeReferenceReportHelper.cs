using System;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base.ReportsV2;
using DevExpress.XtraReports.UI;
using Visa2026.Module.Reports;

namespace Visa2026.Module.Services
{
    public static class ApplicationTypeReferenceReportHelper
    {
        public const string ReportName = "Application Type Reference Report";

        /// <summary>
        /// In-place preview passes the current Application as filter; this report lists all types with codes.
        /// </summary>
        public static void ConfigureForFullTypeList(XtraReport report)
        {
            if (report is not ApplicationTypeReferenceReport)
                return;

            report.FilterString = null;

            foreach (var component in report.ComponentStorage)
            {
                if (component is not CollectionDataSource dataSource)
                    continue;
                if (!string.Equals(
                        dataSource.ObjectTypeName,
                        "Visa2026.Module.BusinessObjects.ApplicationType",
                        StringComparison.Ordinal))
                    continue;

                dataSource.Criteria = CriteriaOperator.Parse("!IsNullOrEmpty(SelectionCode)");
            }
        }
    }
}
