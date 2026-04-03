using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    public partial class ApplicationLetterReport : DevExpress.XtraReports.UI.XtraReport
    {
        public ApplicationLetterReport()
        {
            InitializeComponent();
            LoadBackgroundImage();
        }

        private void LoadBackgroundImage()
        {
            try
            {
                string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports", "clkbackground.jpg");
                if (!File.Exists(imagePath))
                {
                    // Try alternative path
                    imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clkbackground.jpg");
                }
                
                if (File.Exists(imagePath))
                {
                    using (Image img = Image.FromFile(imagePath))
                    {
                        Bitmap backgroundImage = new Bitmap(img);
                        
                        // Apply the same image to all background picture boxes
                        xrPictureBoxBackground.Image = new Bitmap(backgroundImage);
                        xrPictureBoxBackgroundTop.Image = new Bitmap(backgroundImage);
                        xrPictureBoxBackgroundBottom.Image = new Bitmap(backgroundImage);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Background image not found at {imagePath}");
                }
            }
            catch (Exception ex)
            {
                // Log error - background image is optional
                System.Diagnostics.Debug.WriteLine($"Error loading background image: {ex.Message}");
            }
        }
    }
}
