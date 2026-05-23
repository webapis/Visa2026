using DevExpress.Data.Filtering;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Ministry-style printable list of application type selection codes (A4 portrait).
    /// Columns: SelectionCode, NameTm, ApplicationTypeFilter.NameTm.
    /// </summary>
    public partial class ApplicationTypeReferenceReport : DevExpress.XtraReports.UI.XtraReport
    {
        public ApplicationTypeReferenceReport()
        {
            InitializeComponent();
            AppTypeDataSource.Criteria = CriteriaOperator.Parse("!IsNullOrEmpty(SelectionCode)");
            Detail.SortFields.Add(new GroupField("SelectionCode", XRColumnSortOrder.Ascending));
        }
    }
}
