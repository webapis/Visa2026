using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Visa2026.Module.Services.UserReports
{
    /// <summary>Extracts {{placeholder}} tags from .docx files.</summary>
    public interface IUserReportPlaceholderExtractor
    {
        /// <summary>Extract all placeholder keys from a .docx file.</summary>
        /// <param name="docxStream">The .docx file stream</param>
        /// <returns>List of unique placeholder keys (e.g., "ApplicationNumber", "CompanyHead.FullName")</returns>
        Task<IList<string>> ExtractPlaceholdersAsync(Stream docxStream);
    }
}
