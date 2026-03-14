using System;
using System.Collections.Concurrent;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Services
{
    public class ReportVisibilityCacheService : IReportVisibilityCacheService
    {
        private readonly IObjectSpaceFactory _objectSpaceFactory;
        private readonly ConcurrentDictionary<string, ReportVisibility> _cache = new();
        private bool _isInitialized = false;
        private readonly object _lock = new object();

        public ReportVisibilityCacheService(IObjectSpaceFactory objectSpaceFactory)
        {
            _objectSpaceFactory = objectSpaceFactory;
        }

        public ReportVisibility GetReportVisibility(string reportName, Type targetType)
        {
            EnsureInitialized();
            
            string key = GetCacheKey(reportName, targetType.FullName);
            _cache.TryGetValue(key, out var rule);
            
            return rule;
        }

        public void ClearCache()
        {
            lock (_lock)
            {
                _cache.Clear();
                _isInitialized = false;
            }
        }

        private void EnsureInitialized()
        {
            if (_isInitialized) return;

            lock (_lock)
            {
                if (_isInitialized) return;

                using (IObjectSpace os = _objectSpaceFactory.CreateObjectSpace(typeof(ReportVisibility)))
                {
                    var rules = os.GetObjects<ReportVisibility>().ToList();
                    foreach (var rule in rules)
                    {
                        string key = GetCacheKey(rule.ReportName, rule.TargetTypeFullName);
                        _cache[key] = rule;
                    }
                }
                _isInitialized = true;
            }
        }

        private string GetCacheKey(string reportName, string typeFullName) 
            => $"{reportName}|{typeFullName}";
    }
}