using System;
using System.IO;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    public partial class AppBaseReport : XtraReport
    {
        public AppBaseReport()
        {
            InitializeComponent();
            LoadBackground("background.jpg");
        }

        /// <summary>
        /// Loads a background image into xrPictureBoxBackground.
        /// Searches: Resources/FormTemplates/, Reports/FormTemplates/, FormTemplates/, BaseDirectory.
        /// </summary>
        protected void LoadBackground(string fileName)
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
                    return;
                }
            }
            System.Diagnostics.Debug.WriteLine($"[AppBaseReport] Background not found: {fileName}");
        }
    }
}
