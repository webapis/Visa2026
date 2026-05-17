using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.ExpressApp;
using System.Drawing.Text;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services
{
    public static class PdfMappingHelper
    {
        private static string NormalizePdfText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            // Turkish + Turkmen (Latin) → English equivalents (ASCII-ish) + invariant uppercasing.
            // Notes:
            // - Special case: 'ı'/'İ'/'i'/'I' must all become 'I' (dotless).
            // - Some letters expand into digraphs to better match pronunciation: Ç→CH, Ş→SH, Ž→ZH, Ň→NG.
            // - Uppercasing is applied at the end to avoid locale-specific casing quirks.
            string s = value
                // Turkish I variants
                .Replace("ı", "I").Replace("İ", "I").Replace("i", "I")

                // Turkish letters
                .Replace("ç", "ch").Replace("Ç", "ch")
                .Replace("ş", "sh").Replace("Ş", "sh")
                .Replace("ğ", "g").Replace("Ğ", "g")
                .Replace("ü", "u").Replace("Ü", "u")
                .Replace("ö", "o").Replace("Ö", "o")

                // Turkmen Latin letters
                .Replace("ä", "a").Replace("Ä", "a")
                .Replace("ž", "zh").Replace("Ž", "zh")
                .Replace("ň", "ng").Replace("Ň", "ng")
                .Replace("ý", "y").Replace("Ý", "y");

            // Uppercase after transliteration to avoid Turkish-culture casing quirks.
            return s.ToUpperInvariant();
        }

        private static object NormalizePdfValue(object value)
        {
            return value is string s ? NormalizePdfText(s) : value;
        }

        /// <summary>
        /// Resolves a display label to the raw XFA choiceList code using the
        /// provided lookup table. Logs a warning when the value is unrecognised
        /// so the calling code can catch mismatches early.
        /// </summary>
        private static string ResolveRawValue(
            Dictionary<string, string> lookup,
            string displayValue,
            string fieldLabel,
            string fieldKey,
            ILogger logger)
        {
            if (displayValue == null) return null;
            if (lookup.TryGetValue(displayValue, out string raw))
            {
                if (raw != displayValue)
                    logger?.LogDebug(
                        "PDF mapping: [{FieldLabel}] key='{FieldKey}' — resolved display '{Display}' → raw '{Raw}'.",
                        fieldLabel, fieldKey, displayValue, raw);
                return raw;
            }
            logger?.LogWarning(
                "PDF mapping: [{FieldLabel}] key='{FieldKey}' — display value '{Display}' has no known raw mapping. " +
                "Passing value as-is; the field may render blank in the PDF.",
                fieldLabel, fieldKey, displayValue);
            return displayValue; // best-effort pass-through
        }

        private static Image CreateDemoImage()
        {
            var bmp = new Bitmap(150, 200);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(Brushes.LightGray, 0, 0, bmp.Width, bmp.Height);
                g.DrawRectangle(Pens.DarkGray, 0, 0, bmp.Width - 1, bmp.Height - 1);

                var font = new Font("Arial", 16, FontStyle.Bold);
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.DrawString("DEMO\nIMAGE", font, Brushes.DimGray, new RectangleF(0, 0, bmp.Width, bmp.Height), stringFormat);
            }
            return bmp;
        }

        public static IList<PdfFormMappingDefinition> GetMappings(IObjectSpace objectSpace)
        {
            return objectSpace.GetObjectsQuery<PdfFormMapping>()
                .Select(m => new PdfFormMappingDefinition
                {
                    PdfFieldKey = m.PdfFieldKey,
                    Description = m.Description,
                    MappingMode = m.MappingMode,
                    PropertyPath = m.PropertyPath,
                    Expression = m.Expression,
                    ConstantValue = m.ConstantValue
                })
                .ToList();
        }

        public static void RefreshMappingCache(IObjectSpace objectSpace) { }

        // This method is extracted from ApplicationItemPdfController to be reused.
        public static void MapApplicationData(
            Dictionary<string, object> data,
            Application application,
            ApplicationItem item,
            IObjectSpace objectSpace,
            ILogger logger = null,
            IList<PdfFormMappingDefinition> mappings = null,
            ICollection<string> pdfVisibilityGateNotes = null)
        {
            // --- DEBUGGING ---
            // Set to true to bypass database image and use a generated demo image instead.
            const bool useDemoImage = false;
            // -----------------

            void Log(string fieldKey, string fieldLabel, object value)
            {
                if (logger == null) return;
                if (value == null)
                    logger.LogWarning("PDF mapping: [{FieldLabel}] key='{FieldKey}' → NULL (field will be skipped).", fieldLabel, fieldKey);
                else
                    logger.LogDebug("PDF mapping: [{FieldLabel}] key='{FieldKey}' → '{Value}' ({ValueType}).",
                        fieldLabel, fieldKey, value, value.GetType().Name);
            }

            // --- 6. Dynamic Mappings from Database ---
            if (mappings != null)
            {
                foreach (var mapping in mappings)
                {
                    if (string.IsNullOrEmpty(mapping.PdfFieldKey))
                        continue;

                    object val = null;
                    try
                    {
                        switch (mapping.MappingMode)
                        {
                            case PdfMappingMode.Property:
                                if (!string.IsNullOrEmpty(mapping.PropertyPath))
                                {
                                    if (!IsPdfMappingSourceAllowed(application, item, mapping.MappingMode, mapping.PropertyPath, mapping.Expression, logger, pdfVisibilityGateNotes, mapping))
                                        break;
                                    val = GetValueByPath(item, mapping.PropertyPath);
                                }
                                break;
                            case PdfMappingMode.Expression:
                                if (!string.IsNullOrEmpty(mapping.Expression))
                                {
                                    if (!IsPdfMappingSourceAllowed(application, item, mapping.MappingMode, mapping.PropertyPath, mapping.Expression, logger, pdfVisibilityGateNotes, mapping))
                                        break;
                                    var evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(item), CriteriaOperator.Parse(mapping.Expression));
                                    val = evaluator.Evaluate(item);
                                }
                                break;
                            case PdfMappingMode.Constant:
                                val = mapping.ConstantValue;
                                break;
                        }

                        if (val != null)
                        {
                            val = NormalizePdfValue(val);
                            data[mapping.PdfFieldKey] = val;
                            Log(mapping.PdfFieldKey, $"Dynamic Mapping: {mapping.Description}", val);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "PDF mapping: Error processing dynamic mapping for key='{Key}' ('{Description}').", mapping.PdfFieldKey, mapping.Description);
                    }
                }
            }

            logger?.LogDebug("PDF mapping complete. Total keys added to data dictionary: {Count}.", data.Count);
        }

        private static object GetValueByPath(object obj, string path)
        {
            if (obj == null || string.IsNullOrEmpty(path)) return null;

            path = RewriteLegacyApplicationItemPropertyPath(path);

            object current = obj;
            foreach (string part in path.Split('.'))
            {
                if (current == null) return null;
                Type type = current.GetType();
                PropertyInfo info = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (info == null) return null;
                current = info.GetValue(current, null);
            }
            return current;
        }

        /// <summary>
        /// PDF property/expression mappings are skipped unless the same <see cref="ApplicationType"/> flags and
        /// linked objects that drive XAF <see cref="ApplicationItem"/> / <see cref="Application"/> visibility are satisfied
        /// (and required navigations are non-null where the path reads through them).
        /// </summary>
        private static bool IsPdfMappingSourceAllowed(
            Application application,
            ApplicationItem item,
            PdfMappingMode mode,
            string propertyPath,
            string expression,
            ILogger logger,
            ICollection<string> pdfVisibilityGateNotes,
            PdfFormMappingDefinition mappingForNote)
        {
            if (mode == PdfMappingMode.Constant)
                return true;

            var source = mode == PdfMappingMode.Property ? propertyPath : expression;
            if (string.IsNullOrWhiteSpace(source))
                return mode != PdfMappingMode.Property;

            if (!PdfMappingSourceGate.IsAllowed(application, item, source))
            {
                logger?.LogDebug(
                    "PDF mapping: skipped (ApplicationType visibility or unset link): mode={Mode}, source='{Source}'.",
                    mode, source);
                if (pdfVisibilityGateNotes != null && mappingForNote != null)
                {
                    var desc = string.IsNullOrWhiteSpace(mappingForNote.Description)
                        ? "—"
                        : mappingForNote.Description.Trim();
                    pdfVisibilityGateNotes.Add(
                        $"PDF field '{mappingForNote.PdfFieldKey}' ({desc}): not filled on purpose — " +
                        "this rule matches data that is hidden for this application type or not linked on the form " +
                        "(same rules as the on-screen application).");
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Registration/business-trip line fields were merged onto <see cref="ApplicationItem"/>; PDF mappings may
        /// still use <c>CurrentRegistration.*</c> or <c>CurrentBusinessTrip.*</c> from removed navigations.
        /// </summary>
        private static string RewriteLegacyApplicationItemPropertyPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            foreach (var prefix in new[] { "CurrentRegistration.", "CurrentBusinessTrip." })
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return path[prefix.Length..];
            }

            return path;
        }

        private static class PdfMappingSourceGate
        {
            private static bool Token(string source, string token) =>
                !string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(token) &&
                Regex.IsMatch(source, @"\b" + Regex.Escape(token) + @"\b", RegexOptions.CultureInvariant);

            private static bool AppSegment(Application application, string source, string segmentAfterApplicationDot)
            {
                if (application == null)
                    return false;
                return source.IndexOf("Application." + segmentAfterApplicationDot, StringComparison.Ordinal) >= 0;
            }

            public static bool IsAllowed(Application application, ApplicationItem item, string source)
            {
                if (application == null)
                    return false;

                source = RewriteLegacyApplicationItemPropertyPath(source);

                var t = application.ApplicationType;

                bool TypeOk(Func<ApplicationType, bool> show) => t != null && show(t);

                // --- Application.* (mirror Application.cs Appearance + non-null navigations) ---
                if (AppSegment(application, source, "Urgency"))
                {
                    if (!TypeOk(x => x.ShowUrgency) || application.Urgency == null)
                        return false;
                }

                if (AppSegment(application, source, "ProjectContract"))
                {
                    if (!TypeOk(x => x.ShowProjectContract) || application.ProjectContract == null)
                        return false;
                }

                if (AppSegment(application, source, "VisaPeriod"))
                {
                    if (!TypeOk(x => x.ShowVisaPeriod) || application.VisaPeriod == null)
                        return false;
                }

                if (AppSegment(application, source, "VisaCategory"))
                {
                    if (!TypeOk(x => x.ShowVisaCategory) || application.VisaCategory == null)
                        return false;
                }

                if (AppSegment(application, source, "VisaType"))
                {
                    if (!TypeOk(x => x.ShowVisaType) || application.VisaType == null)
                        return false;
                }

                if (AppSegment(application, source, "MigrationService"))
                {
                    if (!TypeOk(x => x.ShowMigrationService) || application.MigrationService == null)
                        return false;
                }

                if (AppSegment(application, source, "BusinessTripStartDate")
                    || AppSegment(application, source, "BusinessTripEndDate")
                    || AppSegment(application, source, "BusinessTripPurpose"))
                {
                    if (!TypeOk(x => x.ShowBusinessTrips))
                        return false;
                }

                if (AppSegment(application, source, "MovementPermitLocation"))
                {
                    if (!TypeOk(x => x.ShowMovementPermitLocation) || application.MovementPermitLocation == null)
                        return false;
                }

                if (AppSegment(application, source, "BorderZoneLocation"))
                {
                    if (!TypeOk(x => x.ShowBorderZoneLocation) || application.BorderZoneLocation == null)
                        return false;
                }

                if (AppSegment(application, source, "FromCity"))
                {
                    if (!TypeOk(x => x.ShowFromCity) || application.FromCity == null)
                        return false;
                }

                if (AppSegment(application, source, "ToCity"))
                {
                    if (!TypeOk(x => x.ShowToCity) || application.ToCity == null)
                        return false;
                }

                if (AppSegment(application, source, "ApplicationType"))
                {
                    if (application.ApplicationType == null)
                        return false;
                }

                if (item == null)
                    return false;

                // --- ApplicationItem: employee-only fields (no ApplicationType flag; mirror ApplyCurrentFieldsFromSelectedPerson) ---
                if (Token(source, "CurrentPositionHistory") || Token(source, "CurrentEmployeeContract"))
                {
                    if (item.Person?.IsEmployee != true)
                        return false;
                }

                if (Token(source, "PreviousPassport"))
                {
                    if (!TypeOk(x => x.ShowPreviousPassport) || item.PreviousPassport == null)
                        return false;
                }

                if (Token(source, "PreviousVisa"))
                {
                    if (!TypeOk(x => x.ShowPreviousVisa) || item.PreviousVisa == null)
                        return false;
                }

                if (Token(source, "CurrentVisaId") || Token(source, "CurrentVisa"))
                {
                    if (!TypeOk(x => x.ShowCurrentVisa) || item.CurrentVisa == null)
                        return false;
                }

                if (Token(source, "PreviousWorkPermitItem") || Token(source, "CurrentWorkPermitItem"))
                {
                    if (!TypeOk(x => x.ShowCurrentWorkPermitItem) || item.Person?.IsEmployee != true)
                        return false;
                }

                if (Token(source, "PreviousWorkPermitItem") && item.PreviousWorkPermitItem == null)
                    return false;

                if (Token(source, "CurrentWorkPermitItem") && item.CurrentWorkPermitItem == null)
                    return false;

                if (Token(source, "CurrentInvitationItem"))
                {
                    if (!TypeOk(x => x.ShowCurrentInvitationItem) || item.CurrentInvitationItem == null)
                        return false;
                }

                if (Token(source, "PreviousInvitationItem"))
                {
                    if (!TypeOk(x => x.ShowPreviousInvitationItem) || item.PreviousInvitationItem == null)
                        return false;
                }

                if (Token(source, "CurrentAddressOfResidence"))
                {
                    if (!TypeOk(x => x.ShowCurrentAddressOfResidence) || item.CurrentAddressOfResidence == null)
                        return false;
                }

                if (Token(source, "CurrentRegistration"))
                {
                    if (!TypeOk(x => x.ShowRegistrations))
                        return false;
                }

                if (Token(source, "MovementRecord") || Token(source, "TravelDate"))
                {
                    if (!TypeOk(x => x.ShowRegistrations) || !item.TravelDate.HasValue)
                        return false;
                }

                if (Token(source, "BusinessTripAddress"))
                {
                    if (!TypeOk(x => x.ShowBusinessTrips) || item.BusinessTripAddress == null)
                        return false;
                }

                if (Token(source, "CurrentEmployeeContract"))
                {
                    if (!TypeOk(x => x.ShowCurrentEmployeeContract) || item.CurrentEmployeeContract == null)
                        return false;
                }

                if (Token(source, "CurrentMedicalRecord"))
                {
                    if (!TypeOk(x => x.ShowCurrentMedicalRecord) || item.CurrentMedicalRecord == null)
                        return false;
                }

                if (Token(source, "CurrentEducation"))
                {
                    if (!TypeOk(x => x.ShowCurrentEducation) || item.CurrentEducation == null)
                        return false;
                }

                if (Token(source, "CurrentPassport") && item.CurrentPassport == null)
                    return false;

                // --- ApplicationItem status columns (mirror ApplicationItem Appearance) ---
                if (Token(source, "InvitationItemIsIssued") && !TypeOk(x => x.ShowInvitationItemIsIssued))
                    return false;
                if (Token(source, "WorkPermitItemIsIssued") && !TypeOk(x => x.ShowWorkPermitItemIsIssued))
                    return false;
                if (Token(source, "RejectionIssued") && !TypeOk(x => x.ShowRejectionIssued))
                    return false;
                if (Token(source, "VisaIssued") && !TypeOk(x => x.ShowVisaIssued))
                    return false;
                if (Token(source, "InvitationItemIsCancelled") && !TypeOk(x => x.ShowInvitationItemIsCancelled))
                    return false;
                if (Token(source, "InvitationItemIsChanged") && !TypeOk(x => x.ShowInvitationItemIsChanged))
                    return false;
                if (Token(source, "WorkPermitItemIsChanged") && !TypeOk(x => x.ShowWorkPermitItemIsChanged))
                    return false;
                if (Token(source, "VisaIsCancelled") && !TypeOk(x => x.ShowVisaIsCancelled))
                    return false;
                if (Token(source, "VisaIsChanged") && !TypeOk(x => x.ShowVisaIsChanged))
                    return false;
                if (Token(source, "IsCancelled") && !TypeOk(x => x.ShowWorkPermitItemIsCancelled))
                    return false;

                return true;
            }
        }
    }

    public class PdfFormMappingDefinition
    {
        public string PdfFieldKey { get; set; }
        public string Description { get; set; }
        public PdfMappingMode MappingMode { get; set; }
        public string PropertyPath { get; set; }
        public string Expression { get; set; }
        public string ConstantValue { get; set; }
    }
}