using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ReportsV2;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    /// <summary>
    /// Ensures <see cref="Reports.ApplicationTypeReferenceReport"/> lists all coded types when previewed from Application.
    /// </summary>
    public class ApplicationTypeReferenceReportController : ObjectViewController<DetailView, Application>
    {
        private IReportDataSourceHelper? _reportDataSourceHelper;

        protected override void OnActivated()
        {
            base.OnActivated();
            _reportDataSourceHelper = Application.ServiceProvider.GetService<IReportDataSourceHelper>();
            if (_reportDataSourceHelper != null)
                _reportDataSourceHelper.BeforeShowPreview += ReportDataSourceHelper_BeforeShowPreview;
        }

        protected override void OnDeactivated()
        {
            if (_reportDataSourceHelper != null)
                _reportDataSourceHelper.BeforeShowPreview -= ReportDataSourceHelper_BeforeShowPreview;
            _reportDataSourceHelper = null;
            base.OnDeactivated();
        }

        private static void ReportDataSourceHelper_BeforeShowPreview(object? sender, BeforeShowPreviewEventArgs e) =>
            ApplicationTypeReferenceReportHelper.ConfigureForFullTypeList(e.Report);
    }
}
