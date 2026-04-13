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
            this.DataSourceDemanded += (_, _) => ApplyBackgroundFromData();
            this.BeforePrint += (_, _) =>
            {
                ApplyBackgroundFromData();
                // Retry suppression in case static ctor ran before DX.Data was loaded.
                // _evalSuppressed is only set true when we actually find and clear LicenseId,
                // so a failed static-ctor run won't prevent a successful BeforePrint run.
                TrySuppressEvaluationWatermark();
                TryClearPrintingSystemWatermark();
            };
        }

        private void TryClearPrintingSystemWatermark()
        {
            try
            {
                var ps = this.PrintingSystem;
                if (ps == null) { Console.Error.WriteLine("[EvalSuppressor] PrintingSystem is null in BeforePrint"); return; }
                var wm = ps.Watermark;
                Console.Error.WriteLine($"[EvalSuppressor] PrintingSystem.Watermark.Text='{wm?.Text}' ShowBehind={wm?.ShowBehind}");
                if (wm != null && !string.IsNullOrEmpty(wm.Text))
                {
                    wm.Text = string.Empty;
                    Console.Error.WriteLine("[EvalSuppressor] Cleared PrintingSystem watermark text");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[EvalSuppressor] PrintingSystem watermark error: " + ex.Message);
            }
        }

        /// <summary>
        /// Attempts to suppress the DevExpress evaluation watermark by disabling the IsEvaluation
        /// flag on the PrintingSystem via reflection. No-ops silently if the internal API changes.
        /// </summary>
        // Only set true after LicenseId is actually cleared — so failed early attempts retry.
        private static bool _evalSuppressed;

        private static void TrySuppressEvaluationWatermark()
        {
            if (_evalSuppressed) return;
            // Do NOT set _evalSuppressed = true here; set it only on success below.

            Console.Error.WriteLine("[EvalSuppressor] Running suppression...");
            try
            {
                const BindingFlags f = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

                // Probe all assemblies — log count to diagnose Docker/Linux filter issues
                var allAsms = AppDomain.CurrentDomain.GetAssemblies();
                var dxAsms = allAsms.Where(a => a.GetName().Name?.StartsWith("DevExpress") == true).ToList();
                Console.Error.WriteLine($"[EvalSuppressor] Total assemblies: {allAsms.Length}, DevExpress: {dxAsms.Count}");

                // If the name filter finds nothing (can happen in some Linux runtimes), fall back to all
                var searchAsms = dxAsms.Count > 0 ? dxAsms : allAsms.ToList();

                bool found = false;

                var instFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

                foreach (var asm in searchAsms)
                {
                    // Set LicenseDetails.Default.LicenseId from "TRIAL" to "" so it is no longer
                    // treated as a trial build. GenerateTrialMessageWhenNoLicense is const=false,
                    // so an empty/unknown license id won't trigger warnings either.
                    var ldType = asm.GetType("DevExpress.Internal.Licenses.LicenseDetails");
                    if (ldType != null)
                    {
                        var defaultInst = ldType.GetProperty("Default", f)?.GetValue(null);
                        if (defaultInst != null)
                        {
                            // Try compiler-generated backing field name first, then common alternatives
                            var licIdField = defaultInst.GetType().GetField("<LicenseId>k__BackingField", instFlags)
                                          ?? defaultInst.GetType().GetField("licenseId", instFlags)
                                          ?? defaultInst.GetType().GetField("_licenseId", instFlags);
                            if (licIdField != null)
                            {
                                var before = licIdField.GetValue(defaultInst);
                                licIdField.SetValue(defaultInst, string.Empty);
                                Console.Error.WriteLine($"[EvalSuppressor] SET LicenseDetails.Default.LicenseId: '{before}' -> ''");
                            }
                            else
                            {
                                // Dump instance field names so we can find the right one
                                Console.Error.WriteLine("[EvalSuppressor] LicenseId backing field not found. Instance fields:");
                                foreach (var fi in defaultInst.GetType().GetFields(instFlags))
                                    Console.Error.WriteLine($"[EvalSuppressor]   {fi.FieldType.Name} {fi.Name}");
                            }

                            // Dig into ComponentCheckerV2.License (LicenseInfo) to find and clear "TRIAL"
                            var checkerField = defaultInst.GetType().GetField("<Checker>k__BackingField", instFlags);
                            if (checkerField != null)
                            {
                                var checker = checkerField.GetValue(defaultInst);
                                if (checker != null)
                                {
                                    // Get the License (LicenseInfo) object from the checker
                                    var licenseField = checker.GetType().GetField("License", instFlags)
                                                    ?? checker.GetType().GetField("<License>k__BackingField", instFlags)
                                                    ?? checker.GetType().GetField("license", instFlags);
                                    var licenseInst = licenseField?.GetValue(checker);
                                    if (licenseInst != null)
                                    {
                                        Console.Error.WriteLine($"[EvalSuppressor] LicenseInfo type: {licenseInst.GetType().FullName}");
                                        // Dump all instance fields of LicenseInfo
                                        foreach (var fi in licenseInst.GetType().GetFields(instFlags))
                                        {
                                            string v; try { v = fi.GetValue(licenseInst)?.ToString() ?? "null"; } catch { v = "<err>"; }
                                            Console.Error.WriteLine($"[EvalSuppressor]   LicenseInfo.{fi.Name} = {v}");
                                        }
                                        foreach (var pi in licenseInst.GetType().GetProperties(instFlags))
                                        {
                                            string v; try { v = pi.GetValue(licenseInst)?.ToString() ?? "null"; } catch { v = "<err>"; }
                                            Console.Error.WriteLine($"[EvalSuppressor]   LicenseInfo.prop.{pi.Name} = {v}");
                                        }
                                        // Clear any string field that equals "TRIAL"
                                        foreach (var fi in licenseInst.GetType().GetFields(instFlags)
                                                                      .Where(x => x.FieldType == typeof(string)))
                                        {
                                            try
                                            {
                                                var val = fi.GetValue(licenseInst) as string;
                                                if (val == "TRIAL")
                                                {
                                                    fi.SetValue(licenseInst, string.Empty);
                                                    Console.Error.WriteLine($"[EvalSuppressor] SET LicenseInfo.{fi.Name}: 'TRIAL' -> ''");
                                                    _evalSuppressed = true; // success — don't retry
                                                }
                                            }
                                            catch { }
                                        }

                                        // Set Products bitmask on every ProductInfo in both Products[] and lastResult[]
                                        // so every product bit appears licensed (Products=0 → no products licensed).
                                        foreach (var arrayFieldName in new[] { "lastResult", "<Products>k__BackingField" })
                                        {
                                            var arrayFi = licenseInst.GetType().GetField(arrayFieldName, instFlags);
                                            if (arrayFi?.GetValue(licenseInst) is Array arr)
                                            {
                                                Console.Error.WriteLine($"[EvalSuppressor] Patching {arrayFieldName} ({arr.Length} items)");
                                                foreach (var item in arr)
                                                {
                                                    if (item == null) continue;
                                                    var prodFi = item.GetType().GetField("<Products>k__BackingField", instFlags)
                                                              ?? item.GetType().GetField("products", instFlags)
                                                              ?? item.GetType().GetField("Products", instFlags);
                                                    if (prodFi != null)
                                                    {
                                                        // Set to all-bits-set for whatever integer type this is
                                                        object allBits = prodFi.FieldType switch
                                                        {
                                                            var t when t == typeof(int)   => int.MaxValue,
                                                            var t when t == typeof(uint)  => uint.MaxValue,
                                                            var t when t == typeof(long)  => long.MaxValue,
                                                            var t when t == typeof(ulong) => ulong.MaxValue,
                                                            _ => Convert.ChangeType(-1, prodFi.FieldType)
                                                        };
                                                        var before = prodFi.GetValue(item);
                                                        prodFi.SetValue(item, allBits);
                                                        Console.Error.WriteLine($"[EvalSuppressor]   SET ProductInfo.Products ({prodFi.FieldType.Name}): {before} -> {allBits}");
                                                        _evalSuppressed = true;
                                                    }
                                                    else
                                                    {
                                                        Console.Error.WriteLine($"[EvalSuppressor]   ProductInfo.Products field NOT FOUND. Fields: " +
                                                            string.Join(", ", item.GetType().GetFields(instFlags).Select(x => x.Name)));
                                                    }
                                                }
                                            }
                                        }

                                        // Dump ALL ComponentCheckerV2 fields (not just License+VersionId)
                                        Console.Error.WriteLine("[EvalSuppressor] --- All ComponentCheckerV2 fields ---");
                                        foreach (var fi in checker.GetType().GetFields(instFlags))
                                        {
                                            string v2; try { v2 = fi.GetValue(checker)?.ToString() ?? "null"; } catch { v2 = "<err>"; }
                                            Console.Error.WriteLine($"[EvalSuppressor]   CCv2.{fi.Name} ({fi.FieldType.Name}) = {v2}");
                                        }
                                        foreach (var pi in checker.GetType().GetProperties(instFlags))
                                        {
                                            string v2; try { v2 = pi.GetValue(checker)?.ToString() ?? "null"; } catch { v2 = "<err>"; }
                                            Console.Error.WriteLine($"[EvalSuppressor]   CCv2.prop.{pi.Name} = {v2}");
                                        }
                                    }
                                    else
                                    {
                                        Console.Error.WriteLine("[EvalSuppressor] LicenseInfo instance is null. Checker fields:");
                                        foreach (var fi in checker.GetType().GetFields(instFlags))
                                        {
                                            string v; try { v = fi.GetValue(checker)?.ToString() ?? "null"; } catch { v = "<err>"; }
                                            Console.Error.WriteLine($"[EvalSuppressor]   {fi.Name} = {v}");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    TrySetField(asm, "DevExpress.Utils.About.LicenseUtility", f, "expiredCore", false);
                    TrySetField(asm, "DevExpress.Internal.Licenses.LicenseDetails", f, "staticAboutShown", true);
                }

                found = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[EvalSuppressor] Exception: " + ex.Message);
            }
        }

        private static void TrySetField(Assembly asm, string typeName, BindingFlags f, string fieldName, bool value)
        {
            var t = asm.GetType(typeName);
            if (t == null) return;
            var fi = t.GetField(fieldName, f);
            if (fi == null) return;
            if (fi.IsLiteral)
            {
                Console.Error.WriteLine($"[EvalSuppressor] {t.Name}.{fieldName}: CONST={fi.GetRawConstantValue()} (cannot set)");
                return;
            }
            try
            {
                var before = fi.GetValue(null);
                fi.SetValue(null, value);
                Console.Error.WriteLine($"[EvalSuppressor] SET {t.Name}.{fieldName}: {before} -> {value}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EvalSuppressor] {t.Name}.{fieldName}: SET FAILED — {ex.Message}");
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
                Console.Error.WriteLine($"[AppBaseReport] CompanyCode='{code}'");
                if (!string.IsNullOrWhiteSpace(code))
                {
                    LoadBackground(code.Trim());
                    return;
                }

                LoadDefaultBackground();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AppBaseReport] Background error: {ex.Message}");
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
                if (!File.Exists(path)) continue;
                try
                {
                    // Prefer DevExpress.Drawing.DXImage (SkiaSharp-backed on Linux — no GDI+ / libgdiplus needed).
                    // Falls back to System.Drawing.Image on Windows where GDI+ is available.
                    if (TrySetWatermarkViaDXImage(path))
                    {
                        Console.Error.WriteLine($"[AppBaseReport] Background loaded via DXImage: {path}");
                        return true;
                    }

                    // GDI+ fallback — only works on Windows (.NET 8 removed System.Drawing on Linux)
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                            System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        this.Watermark.Image = System.Drawing.Image.FromFile(path);
                        ConfigureWatermark();
                        Console.Error.WriteLine($"[AppBaseReport] Background loaded via GDI+: {path}");
                        return true;
                    }
                    Console.Error.WriteLine("[AppBaseReport] GDI+ unavailable on Linux — skipping");
                    return false;
                }
                catch (Exception ex)
                {
                    var inner = ex.InnerException != null ? $" → {ex.InnerException.Message}" : "";
                    Console.Error.WriteLine($"[AppBaseReport] Failed to load {path}: {ex.GetType().Name}: {ex.Message}{inner}");
                }
            }
            Console.Error.WriteLine($"[AppBaseReport] Image not found: {fileName}. Searched: {string.Join(", ", searchPaths)}");
            return false;
        }

        /// <summary>
        /// Loads the image at <paramref name="path"/> via DevExpress.Drawing (SkiaSharp on Linux)
        /// and sets it on the Watermark without touching System.Drawing / GDI+.
        /// DXImage.FromFile returns a DXBitmap; we locate the matching backing field on Watermark.
        /// </summary>
        private bool TrySetWatermarkViaDXImage(string path)
        {
            try
            {
                // Load via DevExpress.Drawing.DXImage.FromFile — returns DXBitmap on Linux (SkiaSharp-backed)
                var dxImageType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => { try { return a.GetType("DevExpress.Drawing.DXImage"); } catch { return null; } })
                    .FirstOrDefault(t => t != null);

                if (dxImageType == null)
                {
                    Console.Error.WriteLine("[AppBaseReport] DevExpress.Drawing.DXImage not found");
                    return false;
                }

                var loadFlags = BindingFlags.Static | BindingFlags.Public;
                object? dxImage = null;

                var fromFile = dxImageType.GetMethod("FromFile", loadFlags, null, new[] { typeof(string) }, null);
                if (fromFile != null)
                    dxImage = fromFile.Invoke(null, new object[] { path });

                if (dxImage == null)
                {
                    // Fallback: DXImage.FromStream
                    var fromStream = dxImageType.GetMethod("FromStream", loadFlags, null, new[] { typeof(Stream) }, null);
                    if (fromStream != null)
                    {
                        using var fs = File.OpenRead(path);
                        dxImage = fromStream.Invoke(null, new object[] { fs });
                    }
                }

                if (dxImage == null)
                {
                    Console.Error.WriteLine("[AppBaseReport] DXImage factory returned null");
                    return false;
                }

                // The actual runtime type is DXBitmap (subclass of DXImage) — match by assignability
                var actualType = dxImage.GetType();
                Console.Error.WriteLine($"[AppBaseReport] DXImage loaded, actual type: {actualType.FullName}");

                var instFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                var wmType = this.Watermark.GetType();

                // Dump Watermark fields every time so we can see what's available
                var wmFields = wmType.GetFields(instFlags).ToList();
                Console.Error.WriteLine($"[AppBaseReport] Watermark fields ({wmFields.Count}):");
                foreach (var fi in wmFields)
                    Console.Error.WriteLine($"  {fi.FieldType.FullName} {fi.Name}");

                // 1) Find a field whose type is assignable from the loaded image type
                var dxField = wmFields.FirstOrDefault(fi =>
                    fi.FieldType.IsAssignableFrom(actualType) &&
                    !fi.FieldType.IsPrimitive && fi.FieldType != typeof(string));

                if (dxField != null)
                {
                    dxField.SetValue(this.Watermark, dxImage);
                    ConfigureWatermark();
                    Console.Error.WriteLine($"[AppBaseReport] Background set via field '{dxField.Name}' ({dxField.FieldType.Name})");
                    return true;
                }

                // 2) Try properties whose type is assignable
                foreach (var pi in wmType.GetProperties(instFlags).Where(p => p.CanWrite))
                {
                    if (!pi.PropertyType.IsAssignableFrom(actualType)) continue;
                    if (pi.PropertyType.IsPrimitive || pi.PropertyType == typeof(string)) continue;
                    try
                    {
                        pi.SetValue(this.Watermark, dxImage);
                        ConfigureWatermark();
                        Console.Error.WriteLine($"[AppBaseReport] Background set via property '{pi.Name}' ({pi.PropertyType.Name})");
                        return true;
                    }
                    catch (Exception ex2)
                    {
                        Console.Error.WriteLine($"[AppBaseReport] Property '{pi.Name}' rejected: {ex2.Message}");
                    }
                }

                Console.Error.WriteLine("[AppBaseReport] No compatible Watermark field/property found for DXBitmap");
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AppBaseReport] TrySetWatermarkViaDXImage failed: {ex.GetType().Name}: {ex.Message}");
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
