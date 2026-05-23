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
                .Select(t => new ApplicationTypeCodePickerRow
                {
                    Id = t.ID,
                    Name = t.Name ?? string.Empty,
                    SelectionCode = t.SelectionCode!,
                    DisplayName = !string.IsNullOrEmpty(t.NameTm) ? t.NameTm : (t.Name ?? string.Empty),
                    CategoryName = GetSelectionCodeGroupName(t.SelectionCode),
                    ReadinessStatus = ApplicationTypeDevelopmentReadiness.GetStatus(t.Name, t.SelectionCode),
                })
                .ToList();
        }

        /// <summary>Ministry table group (hundreds digit of <see cref="ApplicationType.SelectionCode"/>).</summary>
        internal static string GetSelectionCodeGroupName(string? selectionCode)
        {
            if (string.IsNullOrWhiteSpace(selectionCode)
                || selectionCode.Length != 3
                || !int.TryParse(selectionCode, out var code))
            {
                return string.Empty;
            }

            return (code / 100) switch
            {
                1 => "Çakylyk",
                2 => "Gulluk Pasport",
                3 => "Hasaba Alyş",
                4 => "Iş Rugsatnama",
                5 => "Iş Sapary",
                6 => "Serhet ýaka",
                7 => "Wiza",
                8 => "Ýatyrmak",
                _ => string.Empty,
            };
        }
    }
}
