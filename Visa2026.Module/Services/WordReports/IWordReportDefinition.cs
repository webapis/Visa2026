using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.WordReports
{
    /// <summary>
    /// Represents a single Word report that can be generated for an Application.
    /// Register each implementation as IWordReportDefinition in DI.
    /// WordReportsController discovers all registrations and runs applicable ones.
    /// </summary>
    public interface IWordReportDefinition
    {
        /// <summary>
        /// ApplicationType.Name values this report applies to.
        /// Null or empty array = applies to ALL application types.
        /// </summary>
        string[] ApplicableApplicationTypeNames { get; }

        /// <summary>
        /// Secondary applicability check — called only when ApplicationType.Name matches.
        /// Use to guard against empty collections (e.g. no BusinessTrips).
        /// </summary>
        bool IsApplicable(Application application);

        /// <summary>
        /// Output file name (without path), e.g. "Sanawy_CEC-0042_20260512.docx".
        /// </summary>
        string GetFileName(Application application);

        /// <summary>
        /// Fill the template and write the result to outputStream.
        /// </summary>
        Task GenerateAsync(Application application, IWordFormFillerService wordService, Stream outputStream);
    }
}
