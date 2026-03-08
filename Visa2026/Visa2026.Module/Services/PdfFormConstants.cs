using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services
{
    public static class PdfFormConstants
    {
        private static Dictionary<string, Dictionary<string, string>> _cache;
        private static readonly object _lock = new object();

        public static string GetValue(string category, string displayValue, IObjectSpace objectSpace)
        {
            if (string.IsNullOrEmpty(displayValue)) return null;

            EnsureCacheLoaded(objectSpace);

            if (_cache.TryGetValue(category, out var categoryValues))
            {
                if (categoryValues.TryGetValue(displayValue, out string pdfValue))
                {
                    return pdfValue;
                }
            }

            return displayValue; // Fallback to original value if no mapping found
        }

        public static void RefreshCache(IObjectSpace objectSpace)
        {
            lock (_lock)
            {
                _cache = null;
                EnsureCacheLoaded(objectSpace);
            }
        }

        private static void EnsureCacheLoaded(IObjectSpace objectSpace)
        {
            if (_cache != null) return;

            lock (_lock)
            {
                if (_cache != null) return;

                Debug.WriteLine("[PdfFormConstants] Cache miss. Loading constants from database...");
                var constants = objectSpace.GetObjectsQuery<PdfFormConstant>().ToList();
                Debug.WriteLine($"[PdfFormConstants] Loaded {constants.Count} constants.");

                _cache = constants
                    .GroupBy(c => c.Category)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToDictionary(c => c.DisplayValue, c => c.PdfValue, StringComparer.OrdinalIgnoreCase),
                        StringComparer.OrdinalIgnoreCase
                    );
                Debug.WriteLine($"[PdfFormConstants] Cache initialized with categories: {string.Join(", ", _cache.Keys)}");
            }
        }
    }
}