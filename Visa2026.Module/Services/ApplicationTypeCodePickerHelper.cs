using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services
{
    public sealed class ApplicationTypeCodePickerRow
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string SelectionCode { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public ApplicationTypeReadinessStatus ReadinessStatus { get; init; }
        public bool CanSelect => ApplicationTypeDevelopmentReadiness.CanSelectOnApplicationForm(ReadinessStatus);
    }

    public static class ApplicationTypeCodePickerHelper
    {
        public static IList<ApplicationTypeCodePickerRow> LoadRows(IObjectSpace objectSpace)
        {
            ArgumentNullException.ThrowIfNull(objectSpace);

            return objectSpace.GetObjectsQuery<ApplicationType>()
                .Where(t => t.SelectionCode != null && t.SelectionCode != "")
                .OrderBy(t => t.SelectionCode)
                .AsEnumerable()
                .Select(t => new ApplicationTypeCodePickerRow
                {
                    Id = t.ID,
                    Name = t.Name ?? string.Empty,
                    SelectionCode = t.SelectionCode!,
                    DisplayName = LookupLocalization.GetDisplayName(t),
                    CategoryName = GetSelectionCodeGroupDisplayName(t.SelectionCode),
                    ReadinessStatus = ApplicationTypeDevelopmentReadiness.GetStatus(t.Name, t.SelectionCode),
                })
                .ToList();
        }

        /// <summary>Ministry table group (hundreds digit of <see cref="ApplicationType.SelectionCode"/>).</summary>
        internal static string GetSelectionCodeGroupDisplayName(string? selectionCode)
        {
            var groupKey = GetSelectionCodeGroupKey(selectionCode);
            if (string.IsNullOrEmpty(groupKey))
                return string.Empty;

            return LookupLocalization.GetCatalogDisplayName("application-type-group", groupKey);
        }

        internal static string? GetSelectionCodeGroupKey(string? selectionCode)
        {
            if (string.IsNullOrWhiteSpace(selectionCode)
                || selectionCode.Length != 3
                || !int.TryParse(selectionCode, out var code))
            {
                return null;
            }

            return (code / 100) switch
            {
                >= 1 and <= 8 => (code / 100).ToString(),
                _ => null,
            };
        }
    }
}
