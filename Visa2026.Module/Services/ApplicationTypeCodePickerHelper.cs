using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services
{
    public sealed class ApplicationTypeCodePickerRow
    {
        public Guid Id { get; init; }
        public string SelectionCode { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
    }

    public static class ApplicationTypeCodePickerHelper
    {
        public static IList<ApplicationTypeCodePickerRow> LoadRows(IObjectSpace objectSpace)
        {
            ArgumentNullException.ThrowIfNull(objectSpace);

            return objectSpace.GetObjectsQuery<ApplicationType>()
                .Where(t => t.SelectionCode != null && t.SelectionCode != "")
                .OrderBy(t => t.SelectionCode)
                .Select(t => new ApplicationTypeCodePickerRow
                {
                    Id = t.ID,
                    SelectionCode = t.SelectionCode!,
                    DisplayName = !string.IsNullOrEmpty(t.NameTm) ? t.NameTm : (t.Name ?? string.Empty),
                    CategoryName = t.ApplicationTypeFilter != null
                        ? (!string.IsNullOrEmpty(t.ApplicationTypeFilter.NameTm)
                            ? t.ApplicationTypeFilter.NameTm
                            : (t.ApplicationTypeFilter.Name ?? string.Empty))
                        : string.Empty,
                })
                .ToList();
        }
    }
}
