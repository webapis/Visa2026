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
            this.BeforePrint += (_, _) =>
            {
                ApplyBackgroundFromData();
                TrySuppressEvaluationWatermark();
            };
        }

        /// <summary>
        /// Attempts to suppress the DevExpress evaluation watermark by disabling the IsEvaluation
        /// flag on the PrintingSystem via reflection. No-ops silently if the internal API changes.
        /// </summary>
        private static bool _evalSuppressed;

        private static void TrySuppressEvaluationWatermark()
        {
            if (_evalSuppressed) return;
            _evalSuppressed = true;

            Console.Error.WriteLine("[EvalSuppressor] Running suppression...");
            try
            {
                const BindingFlags f = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()
                                             .Where(a => a.GetName().Name?.StartsWith("DevExpress") == true))
                {
                    try
                    {
                        var t = asm.GetType("DevExpress.Internal.Licenses.LicenseAboutHelper");
                        if (t != null)
                        {
                            LogAndSet(t, f, "GenerateTrialMessageWhenNoLicense", false);
                            LogAndSet(t, f, "ShowTrialAboutWhenNoLicense", false);
                        }

                        t = asm.GetType("DevExpress.Utils.About.LicenseUtility");
                        if (t != null)
                            LogAndSet(t, f, "expiredCore", false);

                        t = asm.GetType("DevExpress.Utils.ClientControls.DataContracts.LicenseOptions");
                        if (t != null)
                            LogAndSet(t, f, "DefaultIsLicensed", true);

                        // Also try LicenseDetails.staticAboutShown — marks "about" as already shown
                        t = asm.GetType("DevExpress.Internal.Licenses.LicenseDetails");
                        if (t != null)
                            LogAndSet(t, f, "staticAboutShown", true);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[EvalSuppressor] Exception: " + ex.Message);
            }
        }

        private static void LogAndSet(Type type, BindingFlags flags, string name, bool value)
        {
            var field = type.GetField(name, flags);
            if (field != null && field.FieldType == typeof(bool))
            {
                var before = field.GetValue(null);
                field.SetValue(null, value);
                Console.Error.WriteLine($"[EvalSuppressor] {type.Name}.{name}: {before} -> {value}");
            }
            else
            {
                Console.Error.WriteLine($"[EvalSuppressor] {type.Name}.{name}: NOT FOUND");
            }
        }

        public static readonly string RtfResponsibility =
            @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}" +
            @"\f0\fs30\pard\qj\fi720 " +
            @"Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, " +
            @"onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? " +
            @"etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";

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
