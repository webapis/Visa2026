using System;
using System.IO;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    public partial class AppBaseReport : XtraReport
    {
        private bool _backgroundLoaded;

        public AppBaseReport()
        {
            InitializeComponent();
            // Load default background at construction time (design-time + fallback).
            // Detail.BeforePrint swaps in the company-specific background on the first data row.
            LoadDefaultBackground();
            this.Detail.BeforePrint += Detail_BeforePrint_LoadBackground;
        }

        /// <summary>
        /// Fires before each Detail row renders. GetCurrentColumnValue works here.
        /// Only runs on the first row — subsequent rows keep the already-loaded background.
        /// </summary>
        private void Detail_BeforePrint_LoadBackground(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_backgroundLoaded) return;
            _backgroundLoaded = true;
            try
            {
                var code = GetCurrentColumnValue("Company.Code") as string;
                if (!string.IsNullOrEmpty(code))
                    LoadBackground(code);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppBaseReport] Detail.BeforePrint background error: {ex.Message}");
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
