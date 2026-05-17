using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.UserReports
{
    /// <summary>Generates Word documents from user-defined templates using DocxTemplater.</summary>
    public interface IUserReportGenerator
    {
        /// <summary>Generate a Word document from a user-defined template.</summary>
        /// <param name="template">The user-defined template</param>
        /// <param name="application">The application to fill data from</param>
        /// <param name="outputStream">Output stream for the generated document</param>
        Task GenerateAsync(
            UserReportTemplate template,
            Application application,
            Stream outputStream,
            IList<ApplicationItem>? applicationItems = null);

        /// <summary>Generate a Word document from a user-defined template for a specific ApplicationItem.</summary>
        /// <param name="template">The user-defined template</param>
        /// <param name="applicationItem">The application item to fill data from</param>
        /// <param name="outputStream">Output stream for the generated document</param>
        Task GenerateAsync(UserReportTemplate template, ApplicationItem applicationItem, Stream outputStream);
    }
}
