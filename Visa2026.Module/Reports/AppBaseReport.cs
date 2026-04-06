using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    public partial class AppBaseReport : XtraReport
    {
        public AppBaseReport()
        {
            InitializeComponent();
            // Prefer DataSourceDemanded for data-dependent settings (Blazor preview can trigger BeforePrint
            // before the CollectionDataSource is fully populated).
            this.DataSourceDemanded += (_, _) => ApplyBackgroundFromData();
            this.BeforePrint += (_, _) => ApplyBackgroundFromData();
        }

        private void ApplyBackgroundFromData()
        {
            try
            {
                var code = TryGetCompanyCode();
                if (!string.IsNullOrWhiteSpace(code))
                {
                    LoadBackground(code.Trim());
                    return;
                }

                LoadDefaultBackground();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppBaseReport] Background error: {ex}");
                LoadDefaultBackground();
            }
        }

        private string? TryGetCompanyCode()
        {
            // The designer defines AppDataSource (CollectionDataSource) as a private field in the same partial class.
            // In practice, DataSource can be swapped by the reporting pipeline; AppDataSource is the most stable source.
            var dsCandidate = (object?)this.DataSource ?? this.AppDataSource;

            // 1) IListSource (CollectionDataSource implements this)
            if (dsCandidate is IListSource listSource)
            {
                var list = listSource.GetList();
                if (list != null && list.Count > 0)
                    return TryExtractCompanyCode(list[0]);
            }

            // 2) IEnumerable fallback
            if (dsCandidate is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
                        return TryExtractCompanyCode(item);
                    break;
                }
            }

            return null;
        }

        private static string? TryExtractCompanyCode(object row)
        {
            // Most common case: EF proxy types still inherit the entity class, so reflection works reliably.
            // We avoid hard-casting to Application to keep this base usable even if the runtime row type is proxied.
            var company = GetPropertyValue(row, "Company");
            if (company == null) return null;
            var code = GetPropertyValue(company, "Code") as string;
            return code;
        }

        private static object? GetPropertyValue(object instance, string propertyName)
        {
            var type = instance.GetType();
            var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return prop?.GetValue(instance);
        }

        /// <summary>
        /// Loads a company-specific background using Company.Code (e.g. "CLK", "GAP").
        /// Falls back to background.jpg if not found.
        /// Call from a derived constructor when the layout itself differs per company.
        /// </summary>
        protected void LoadBackground(string companyCode)
        {
            if (!TryLoadImage($"background_{companyCode}.jpg"))
            {
                System.Diagnostics.Debug.WriteLine($"[AppBaseReport] background_{companyCode}.jpg not found — using default.");
                LoadDefaultBackground();
            }
        }

        private void LoadDefaultBackground()
        {
            TryLoadImage("background.jpg");
        }

        private bool TryLoadImage(string fileName)
        {
            var searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "FormTemplates", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "FormTemplates", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FormTemplates", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
            };
            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    this.Watermark.Image = System.Drawing.Image.FromFile(path);
                    this.Watermark.ImageViewMode = DevExpress.XtraPrinting.Drawing.ImageViewMode.Stretch;
                    this.Watermark.ImageTransparency = 0;
                    this.Watermark.ShowBehind = true;
                    System.Diagnostics.Debug.WriteLine($"[AppBaseReport] Background loaded: {path}");
                    return true;
                }
            }
            System.Diagnostics.Debug.WriteLine($"[AppBaseReport] Image not found: {fileName}");
            return false;
        }
    }
}
