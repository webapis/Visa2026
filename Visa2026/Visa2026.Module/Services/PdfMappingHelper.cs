using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.ExpressApp;
using System.Drawing.Text;
using System.Reflection;
using Visa2026.Module.BusinessObjects;
using System.Linq;

namespace Visa2026.Module.Services
{
    internal static class PdfMappingHelper
    {
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

        private static List<PdfFormMappingDefinition> _mappingCache;
        private static readonly object _mappingLock = new object();

        public static IList<PdfFormMappingDefinition> GetMappings(IObjectSpace objectSpace)
        {
            if (_mappingCache != null) return _mappingCache;
            lock (_mappingLock)
            {
                if (_mappingCache != null) return _mappingCache;

                _mappingCache = objectSpace.GetObjectsQuery<PdfFormMapping>()
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
            return _mappingCache;
        }

        public static void RefreshMappingCache(IObjectSpace objectSpace)
        {
            lock (_mappingLock)
            {
                _mappingCache = null;
                GetMappings(objectSpace);
            }
        }

        // This method is extracted from ApplicationItemPdfController to be reused.
        public static void MapApplicationData(
            Dictionary<string, object> data,
            Application application,
            ApplicationItem item,
            IObjectSpace objectSpace,
            ILogger logger = null,
            IList<PdfFormMappingDefinition> mappings = null)
        {
            // --- DEBUGGING ---
            // Set to true to bypass database image and use a generated demo image instead.
            const bool useDemoImage = true;
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
                                    val = GetValueByPath(item, mapping.PropertyPath);
                                }
                                break;
                            case PdfMappingMode.Expression:
                                if (!string.IsNullOrEmpty(mapping.Expression))
                                {
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

            object current = obj;
            foreach (string part in path.Split('.'))
            {
                if (current == null) return null;
                Type type = current.GetType();
                PropertyInfo info = type.GetProperty(part);
                if (info == null) return null;
                current = info.GetValue(current, null);
            }
            return current;
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