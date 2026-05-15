using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ExcelReports;

/// <summary>Merges user Excel templates with application data.</summary>
public interface IExcelReportGenerator
{
    /// <summary>List merge from an <see cref="Application"/> (v1).</summary>
    Task GenerateAsync(UserReportTemplate template, Application application, Stream outputStream);

    /// <summary>Single-item merge (v1.1).</summary>
    Task GenerateAsync(UserReportTemplate template, ApplicationItem applicationItem, Stream outputStream);
}
