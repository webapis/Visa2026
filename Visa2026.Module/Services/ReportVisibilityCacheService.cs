using System;
using System.Collections.Concurrent;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services
{
    public class ReportVisibilityCacheService : IReportVisibilityCacheService
    {
        private readonly ConcurrentDictionary<string, ReportVisibility> _cache = new ConcurrentDictionary<string, ReportVisibility>();

        private string GetCacheKey(string reportName, Type targetType)
        {
            return $"{reportName}-{targetType?.FullName}";
        }

        public ReportVisibility GetReportVisibility(string reportName, Type targetType)
        {
            string cacheKey = GetCacheKey(reportName, targetType);
            _cache.TryGetValue(cacheKey, out ReportVisibility reportVisibility);
            return reportVisibility;
        }

        public void InvalidateCache(string reportName, Type targetType)
        {
            string cacheKey = GetCacheKey(reportName, targetType);
            _cache.TryRemove(cacheKey, out _);
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        public void AddToCache(ReportVisibility reportVisibility)
        {
            if (reportVisibility == null) return;
            string cacheKey = GetCacheKey(reportVisibility.ReportName, reportVisibility.TargetType);
            _cache.AddOrUpdate(
                cacheKey,
                reportVisibility,
                (key, oldValue) => reportVisibility); // Update if already exists
        }
    }
}
