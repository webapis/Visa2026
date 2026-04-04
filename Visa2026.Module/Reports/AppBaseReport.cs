using System;
using System.Collections;
using System.IO;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    public partial class AppBaseReport : XtraReport
    {
        public AppBaseReport()
        {
            InitializeComponent();
            // Load default background at construction time (design-time + fallback).
            // BeforePrint will reload with the company-specific background once data is available.
            LoadDefaultBackground();
            this.BeforePrint += AppBaseReport_BeforePrint;
        }

        /// <summary>
        /// Fires before the report prints. At this point the CollectionDataSource
        /// has been filled by XAF, so we can read Company.Code from the first record
        /// and swap in the matching background_{code}.jpg.
        /// Falls back to the already-loaded background.jpg if Company.Code is absent.
        /// </summary>
        private void AppBaseReport_BeforePrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            try
            {
                if (DataSource is IEnumerable items)
                {
                    foreach (var item in items)
                    {
                        if (item is Visa2026.Module.BusinessObjects.Application app)
                        {
                            var code = app.Company?.Code;
                            if (!string.IsNullOrEmpty(code))
                                LoadBackground(code);
                        }
                        break; // Only the first record determines the company background
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppBaseReport] BeforePrint background error: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a company-specific background using Company.Code (e.g. "CLK", "GAP").
        /// Looks for background_{companyCode}.jpg — falls back to background.jpg if not found.
        /// Only call directly from a derived constructor when the layout itself
        /// differs per company (not just the background).
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
                    xrPictureBoxBackground.Image = System.Drawing.Image.FromFile(path);
                    return true;
                }
            }
            System.Diagnostics.Debug.WriteLine($"[AppBaseReport] Image not found: {fileName}");
            return false;
        }
    }
}
