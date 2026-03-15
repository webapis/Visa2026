using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Module_Interface;

namespace Visa2026.Module.Services
{
    public class MailMergeVisibilityCacheService : IMailMergeVisibilityCacheService
    {
        private readonly IObjectSpaceFactory _objectSpaceFactory;
        private readonly ConcurrentDictionary<string, List<MailMergeVisibility>> _cache = new();
        private bool _isInitialized = false;
        private readonly object _lock = new object();

        public MailMergeVisibilityCacheService(IObjectSpaceFactory objectSpaceFactory)
        {
            _objectSpaceFactory = objectSpaceFactory;
        }

        public IEnumerable<MailMergeVisibility> GetVisibilityRules(string templateName, Type targetType)
        {
            EnsureInitialized();
            
            // Key format: "TemplateName|Full.Namespace.Type"
            string key = GetCacheKey(templateName, targetType.FullName);
            return _cache.TryGetValue(key, out var rules) ? rules : Enumerable.Empty<MailMergeVisibility>();
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

                using (IObjectSpace os = _objectSpaceFactory.CreateObjectSpace(typeof(MailMergeVisibility)))
                {
                    var rules = os.GetObjects<MailMergeVisibility>().ToList();
                    foreach (var rule in rules)
                    {
                        string key = GetCacheKey(rule.TemplateName?.Trim(), rule.TargetTypeFullName);
                        if (!_cache.ContainsKey(key))
                        {
                            _cache[key] = new List<MailMergeVisibility>();
                        }
                        _cache[key].Add(rule);
                    }
                }
                _isInitialized = true;
            }
        }

        private string GetCacheKey(string templateName, string typeFullName) 
            => $"{templateName}|{typeFullName}";
    }
}