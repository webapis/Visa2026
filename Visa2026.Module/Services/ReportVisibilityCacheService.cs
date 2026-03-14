using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Services
{
    public class ReportVisibilityCacheService : IReportVisibilityCacheService
    {
        private readonly IObjectSpaceFactory _objectSpaceFactory;
        private readonly ConcurrentDictionary<string, List<ReportVisibility>> _cache = new();
        private bool _isInitialized = false;
        private readonly object _lock = new object();

        public ReportVisibilityCacheService(IObjectSpaceFactory objectSpaceFactory)
        {
            _objectSpaceFactory = objectSpaceFactory;
        }

        public IEnumerable<ReportVisibility> GetReportVisibilities(string reportName, Type targetType)
        {
            EnsureInitialized();
            
            string key = GetCacheKey(reportName, targetType.FullName);
            return _cache.TryGetValue(key, out var rules) ? rules : Enumerable.Empty<ReportVisibility>();
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
                        if (!_cache.ContainsKey(key))
                        {
                            _cache[key] = new List<ReportVisibility>();
                        }
                        _cache[key].Add(rule);
                    }
                }
                _isInitialized = true;
            }
        }

        private string GetCacheKey(string reportName, string typeFullName) 
            => $"{reportName}|{typeFullName}";
    }
}