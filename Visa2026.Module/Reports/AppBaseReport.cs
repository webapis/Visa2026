using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    public partial class AppBaseReport : XtraReport
    {
        public AppBaseReport()
        {
            InitializeComponent();
            // Subscribe at report level — fires before any page rendering begins.
            // At this point XAF has already filled the CollectionDataSource.
            this.BeforePrint += AppBaseReport_BeforePrint;
        }

        /// <summary>
        /// Fires once before the report document starts rendering.
        /// XAF fills CollectionDataSource before calling CreateDocument, so data is available here.
        /// We access it via IListSource (the standard .NET data-binding interface).
        /// Setting Watermark here ensures it covers every page before the first page is rendered.
        /// </summary>
        private void AppBaseReport_BeforePrint(object sender, CancelEventArgs e)
        {
            try
            {
                string code = null;

                // IListSource is the standard way to enumerate data from a binding source component
                if (DataSource is IListSource listSource)
                {
                    var list = listSource.GetList();
                    if (list.Count > 0 && list[0] is Visa2026.Module.BusinessObjects.Application app)
                        code = app.Company?.Code;
                }

                if (!string.IsNullOrEmpty(code))
                    LoadBackground(code);
                else
                    LoadDefaultBackground();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppBaseReport] BeforePrint background error: {ex.Message}");
                LoadDefaultBackground();
            }
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
                    Console.WriteLine($"[AppBaseReport] Background loaded: {path}");
                    return true;
                }
            }
            System.Diagnostics.Debug.WriteLine($"[AppBaseReport] Image not found: {fileName}");
            Console.WriteLine($"[AppBaseReport] Image not found: {fileName}");
            return false;
        }
    }
}
