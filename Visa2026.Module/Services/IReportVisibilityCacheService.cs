using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services
{
    public interface IReportVisibilityCacheService
    {
        ReportVisibility GetReportVisibility(string reportName, Type targetType);

        void InvalidateCache(string reportName, Type targetType);

        void ClearCache();

        void AddToCache(ReportVisibility reportVisibility);


    }
}
