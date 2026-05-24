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
        // Static constructor runs when the class is first referenced — before any instance
        // is created and before CreateDocument() is ever called. This is the earliest possible
        // hook point for the license suppression.
        static AppBaseReport()
        {
            TrySuppressEvaluationWatermark();
        }

        public AppBaseReport()
        {
            InitializeComponent();
            // Prefer DataSourceDemanded for data-dependent settings (Blazor preview can trigger BeforePrint
            // before the CollectionDataSource is fully populated).
            this.DataSourceDemanded += (_, _) =>
            {
                ApplyBackgroundFromData();
            };
            this.BeforePrint += (_, _) =>
            {
                ApplyBackgroundFromData();
                // Retry suppression in case static ctor ran before DX.Data was loaded.
                // _evalSuppressed is only set true when Products bitmask is actually patched.
                TrySuppressEvaluationWatermark();
                TryClearPrintingSystemWatermark();
            };
        }

        private void TryClearPrintingSystemWatermark()
        {
            try
            {
                var wm = this.PrintingSystem?.Watermark;
                if (wm != null && !string.IsNullOrEmpty(wm.Text))
                    wm.Text = string.Empty;
            }
            catch { }
        }

        // Only set true after ProductInfo.Products is actually patched — so failed early attempts retry.
        private static bool _evalSuppressed;

        /// <summary>
        /// Suppresses the DevExpress evaluation watermark on Linux/Docker by patching the internal
        /// license state via reflection. No-ops silently if the internal API changes.
        /// See docs/dx-watermark-suppression.md for the full root-cause chain.
        /// </summary>
        private static void TrySuppressEvaluationWatermark()
        {
            if (_evalSuppressed) return;

            try
            {
                const BindingFlags f = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
                var instFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

                var dxAsms = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name?.StartsWith("DevExpress") == true).ToList();
                var searchAsms = dxAsms.Count > 0 ? dxAsms
                    : AppDomain.CurrentDomain.GetAssemblies().ToList();

                foreach (var asm in searchAsms)
                {
                    var ldType = asm.GetType("DevExpress.Internal.Licenses.LicenseDetails");
                    if (ldType == null) continue;

                    var defaultInst = ldType.GetProperty("Default", f)?.GetValue(null);
                    if (defaultInst == null) continue;

                    // Navigate: LicenseDetails.Default → ComponentCheckerV2 → LicenseInfo
                    var checker = defaultInst.GetType()
                        .GetField("<Checker>k__BackingField", instFlags)?.GetValue(defaultInst);
                    if (checker == null) continue;

                    var licenseInst = (checker.GetType().GetField("License", instFlags)
                                    ?? checker.GetType().GetField("<License>k__BackingField", instFlags)
                                    ?? checker.GetType().GetField("license", instFlags))
                                    ?.GetValue(checker);
                    if (licenseInst == null) continue;

                    // Clear LicenseId ("TRIAL" → "") so DX no longer treats this as a trial build
                    foreach (var fi in licenseInst.GetType().GetFields(instFlags)
                                                  .Where(x => x.FieldType == typeof(string)))
                    {
                        try
                        {
                            if (fi.GetValue(licenseInst) as string == "TRIAL")
                                fi.SetValue(licenseInst, string.Empty);
                        }
                        catch { }
                    }

                    // THE REAL GATE: set Products bitmask to all-bits-set in both arrays.
                    // Products=0 means nothing licensed and triggers the watermark stamp.
                    foreach (var arrayFieldName in new[] { "lastResult", "<Products>k__BackingField" })
                    {
                        if (licenseInst.GetType().GetField(arrayFieldName, instFlags)
                                       ?.GetValue(licenseInst) is not Array arr) continue;

                        foreach (var item in arr)
                        {
                            if (item == null) continue;
                            var prodFi = item.GetType().GetField("<Products>k__BackingField", instFlags)
                                      ?? item.GetType().GetField("products", instFlags)
                                      ?? item.GetType().GetField("Products", instFlags);
                            if (prodFi == null) continue;

                            object allBits = prodFi.FieldType switch
                            {
                                var t when t == typeof(int)   => int.MaxValue,
                                var t when t == typeof(uint)  => uint.MaxValue,
                                var t when t == typeof(long)  => long.MaxValue,
                                var t when t == typeof(ulong) => ulong.MaxValue,
                                _ => Convert.ChangeType(-1, prodFi.FieldType)
                            };
                            prodFi.SetValue(item, allBits);
                            _evalSuppressed = true;
                        }
                    }

                    TrySetField(asm, "DevExpress.Utils.About.LicenseUtility", f, "expiredCore", false);
                    TrySetField(asm, "DevExpress.Internal.Licenses.LicenseDetails", f, "staticAboutShown", true);
                }
            }
            catch { }
        }

        private static void TrySetField(Assembly asm, string typeName, BindingFlags f, string fieldName, bool value)
        {
            var t = asm.GetType(typeName);
            if (t == null) return;
            var fi = t.GetField(fieldName, f);
            if (fi == null || fi.IsLiteral) return;
            try { fi.SetValue(null, value); } catch { }
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
            catch
            {
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
            var code = GetPropertyValue(row, "Company_Code") as string;
            if (!string.IsNullOrWhiteSpace(code))
                return code;

            var profileCode = GetPropertyValue(row, "Application_Company_Code") as string;
            if (!string.IsNullOrWhiteSpace(profileCode))
                return profileCode;

            var company = GetPropertyValue(row, "Company");
            if (company == null) return null;
            return GetPropertyValue(company, "Code") as string;
        }

        private static object? GetPropertyValue(object instance, string propertyName)
        {
            var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return prop?.GetValue(instance);
        }

        /// <summary>
        /// Loads a company-specific background using Company.Code (e.g. "CLK", "GAP").
        /// Falls back to background.jpg if not found.
        /// </summary>
        protected void LoadBackground(string companyCode)
        {
            if (!TryLoadImage($"background_{companyCode}.jpg"))
                LoadDefaultBackground();
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
                if (!File.Exists(path)) continue;
                try
                {
                    // Use ImageSource (DX's native Watermark container — SkiaSharp-backed on Linux).
                    // Falls back to System.Drawing.Image on Windows where GDI+ is available.
                    if (TrySetWatermarkViaImageSource(path))
                        return true;

                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                            System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        this.Watermark.Image = System.Drawing.Image.FromFile(path);
                        ConfigureWatermark();
                        return true;
                    }
                    return false;
                }
                catch { }
            }
            return false;
        }

        /// <summary>
        /// Creates a DevExpress.XtraPrinting.Drawing.ImageSource from the file and assigns it to
        /// the Watermark's ImageSource property. Works on Linux (no System.Drawing/GDI+ required).
        /// </summary>
        private bool TrySetWatermarkViaImageSource(string path)
        {
            try
            {
                var staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                var instFlags   = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

                // Find DevExpress.XtraPrinting.Drawing.ImageSource
                Type? imageSourceType = null;
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try { imageSourceType = a.GetType("DevExpress.XtraPrinting.Drawing.ImageSource"); } catch { }
                    if (imageSourceType != null) break;
                }
                if (imageSourceType == null) return false;

                // Create ImageSource — try ImageSource.FromFile first, then FromBytes, then ctor(DXImage)
                object? imageSource = null;

                var fromFile = imageSourceType.GetMethod("FromFile", staticFlags, null, new[] { typeof(string) }, null);
                if (fromFile != null)
                    imageSource = fromFile.Invoke(null, new object[] { path });

                if (imageSource == null)
                {
                    var fromBytes = imageSourceType.GetMethod("FromBytes", staticFlags, null, new[] { typeof(byte[]) }, null);
                    if (fromBytes != null)
                        imageSource = fromBytes.Invoke(null, new object[] { File.ReadAllBytes(path) });
                }

                if (imageSource == null)
                {
                    var dxImageType = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => { try { return a.GetType("DevExpress.Drawing.DXImage"); } catch { return null; } })
                        .FirstOrDefault(t => t != null);
                    if (dxImageType != null)
                    {
                        var dxFromFile = dxImageType.GetMethod("FromFile", BindingFlags.Static | BindingFlags.Public,
                            null, new[] { typeof(string) }, null);
                        var dxBitmap = dxFromFile?.Invoke(null, new object[] { path });
                        if (dxBitmap != null)
                        {
                            var ctorDx = imageSourceType.GetConstructor(new[] { dxImageType });
                            if (ctorDx != null)
                                imageSource = ctorDx.Invoke(new object[] { dxBitmap });
                        }
                    }
                }

                if (imageSource == null) return false;

                // Set on Watermark.ImageSource — walk the type hierarchy (private fields not inherited)
                for (var t = this.Watermark.GetType(); t != null && t != typeof(object); t = t.BaseType)
                {
                    var prop = t.GetProperty("ImageSource", instFlags);
                    if (prop != null && prop.CanWrite && prop.PropertyType.IsAssignableFrom(imageSourceType))
                    {
                        prop.SetValue(this.Watermark, imageSource);
                        ConfigureWatermark();
                        return true;
                    }

                    foreach (var fi in t.GetFields(instFlags))
                    {
                        if (!fi.FieldType.IsAssignableFrom(imageSourceType)) continue;
                        if (fi.FieldType.IsPrimitive || fi.FieldType == typeof(string)) continue;
                        fi.SetValue(this.Watermark, imageSource);
                        ConfigureWatermark();
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private void ConfigureWatermark()
        {
            this.Watermark.ImageViewMode = DevExpress.XtraPrinting.Drawing.ImageViewMode.Stretch;
            this.Watermark.ImageTransparency = 0;
            this.Watermark.ShowBehind = true;
        }
    }
}
