using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Company Excel layout for one Report Dashboard subreport.
/// Officers export into this file. An administrator replaces the file.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
public class ReportDashboardExcelTemplate : BaseObject
{
    public virtual ReportDashboardCategory Category { get; set; }

    [MaxLength(80)]
    public virtual string SubReportKey { get; set; } = string.Empty;

    [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual FileData? TemplateFile { get; set; }
}
