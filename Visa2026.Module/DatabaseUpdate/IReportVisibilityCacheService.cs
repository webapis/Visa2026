using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Module_Interface
{
    public interface IReportVisibilityCacheService
    {
        /// <summary>
        /// Retrieves the visibility configuration for a specific report and target type.
        /// </summary>
        ReportVisibility GetReportVisibility(string reportName, Type targetType);

        /// <summary>
        /// Forces the cache to be cleared and reloaded on the next request.
        /// </summary>
        void ClearCache();
    }
}