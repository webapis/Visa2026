using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spire.Pdf;
using Spire.Pdf.Fields;
using Spire.Pdf.Widget;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;

namespace Visa2026.Module.Services
{
    public class PdfFormFillerService : IPdfFormFillerService
    {
        private readonly ILogger<PdfFormFillerService> _logger;

        public PdfFormFillerService(ILogger<PdfFormFillerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void FillForm(string templatePath, Stream outputStream, Dictionary<string, object> data)
        {
            if (string.IsNullOrEmpty(templatePath)) throw new ArgumentException("Template path cannot be null or empty.", nameof(templatePath));
            if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));
            if (data == null) throw new ArgumentNullException(nameof(data), "Data dictionary cannot be null.");

            if (!File.Exists(templatePath))
            {
                _logger.LogError($"Template PDF file not found at {templatePath}");
                throw new FileNotFoundException($"Template PDF file not found at {templatePath}", templatePath);
            }

            // Track streams that must stay alive until after SaveToStream
            var streamsToDispose = new List<MemoryStream>();

            try
            {
                PdfDocument pdfdoc = new PdfDocument();
                pdfdoc.LoadFromFile(templatePath);
                PdfFormWidget form = pdfdoc.Form as PdfFormWidget;

                if (form == null)
                {
                    _logger.LogError("PDF document does not contain a form.");
                    throw new InvalidOperationException("PDF document does not contain a form.");
                }

                var _pendingImageBase64 = new Dictionary<string, string>();

                if (form.XFAForm != null)
                {
                    List<XfaField> loFields = form.XFAForm.XfaFields;
                    _logger.LogDebug("XFA form detected. Total fields found: {FieldCount}. Data keys provided: {DataCount}.",
                        loFields.Count, data.Count);
                    // Logging every XFA field name can generate huge strings and cause memory pressure (especially in small containers).
                    // Keep this as a compact summary; include a small sample only when Debug logging is enabled.
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        const int sampleSize = 25;
                        var sample = loFields.Count <= sampleSize ? loFields : loFields.GetRange(0, sampleSize);
                        _logger.LogDebug("XFA field names sample ({SampleCount}/{Total}): [{FieldNames}]",
                            sample.Count,
                            loFields.Count,
                            string.Join(", ", sample.ConvertAll(f => $"{f.Name}({f.GetType().Name})")));
                    }

                    foreach (var field in loFields)
                    {
                        if (data.TryGetValue(field.Name, out object value) && value != null)
                        {
                            _logger.LogDebug("Filling field '{FieldName}' (type: {FieldType}).",
                                field.Name, field.GetType().Name);

                            try
                            {
                                if (field is XfaTextField textField)
                                {
                                    if (value is DateTime dt)
                                    {
                                        textField.Value = dt.ToString("dd.MM.yyyy");
                                    }
                                    else
                                    {
                                        textField.Value = value.ToString();
                                    }
                                }
                                else if (field is XfaDateTimeField dateTimeField)
                                {
                                    if (value is DateTime dt)
                                    {
                                        dateTimeField.Value = dt.ToString("dd.MM.yyyy");
                                    }
                                    else
                                    {
                                        dateTimeField.Value = value.ToString();
                                    }
                                }
                                else if (field is XfaCheckButtonField checkButtonField && value is bool b)
                                {
                                    checkButtonField.Checked = b;
                                }
                                else if (field is XfaChoiceListField choiceListField)
                                {
                                    choiceListField.SelectedItem = value.ToString();
                                }
                                else if (field is XfaImageField imageField)
                                {
                                    _logger.LogInformation("Image field '{FieldName}' found. Value type: {ValueType}. Value is null: {IsNull}.",
                                        field.Name, value?.GetType().FullName ?? "null", value == null);

                                    byte[] imageBytes = null;

                                    try
                                    {
                                        if (value is byte[] rawBytes)
                                        {
                                            // Attempt to convert to PNG to ensure compatibility
                                            try
                                            {
                                                using (var ms = new MemoryStream(rawBytes))
                                                using (var img = Image.FromStream(ms))
                                                using (var tmp = new MemoryStream())
                                                {
                                                    img.Save(tmp, ImageFormat.Png);
                                                    imageBytes = tmp.ToArray();
                                                }
                                                _logger.LogDebug("Image field '{FieldName}': Converted raw bytes to PNG. Orig={OrigLen}, New={NewLen}.",
                                                    field.Name, rawBytes.Length, imageBytes.Length);
                                            }
                                            catch (Exception convEx)
                                            {
                                                _logger.LogWarning(convEx, "Image field '{FieldName}': Failed to convert raw bytes to PNG. Using raw bytes as fallback.", field.Name);
                                                imageBytes = rawBytes;
                                            }
                                        }
                                        else if (value is Image imageObj)
                                        {
                                            using (var tmp = new MemoryStream())
                                            {
                                                imageObj.Save(tmp, ImageFormat.Png);
                                                imageBytes = tmp.ToArray();
                                            }
                                            _logger.LogDebug("Image field '{FieldName}': Converted Image object to PNG bytes. Length={Length}.",
                                                field.Name, imageBytes.Length);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Image field '{FieldName}': unsupported value type '{ValueType}'. " +
                                                "Expected byte[] or System.Drawing.Image.",
                                                field.Name, value?.GetType().FullName ?? "null");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Image field '{FieldName}': Error preparing image data.", field.Name);
                                    }

                                    if (imageBytes != null && imageBytes.Length > 0)
                                    {
                                        // XFA image fields require a data-URI prefix so the renderer
                                        // knows the content type. Raw base64 alone is silently ignored.
                                        string b64 = Convert.ToBase64String(imageBytes);
                                        imageField.ImageValueBase64 = "data:image/png;base64," + b64;
                                        _pendingImageBase64[field.Name] = b64;
                                        _logger.LogInformation("Image field '{FieldName}': ImageValueBase64 set (data:image/png;base64, {Bytes} bytes).",
                                            field.Name, imageBytes.Length);

                                        // Also set Image property for non-flatten code paths.
                                        var imgStream = new MemoryStream(imageBytes);
                                        streamsToDispose.Add(imgStream);
                                        try
                                        {
                                            imageField.Image = Image.FromStream(imgStream);
                                        }
                                        catch (Exception imgEx)
                                        {
                                            _logger.LogWarning(imgEx, "Image field '{FieldName}': Image.FromStream failed (ImageValueBase64 already set).", field.Name);
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Image field '{FieldName}': byte payload is null or empty — skipping assignment.",
                                            field.Name);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error setting field {FieldName} with value {FieldValue}", field.Name, value);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Field '{FieldName}' (type: {FieldType}) has no matching data key or value is null — skipped.",
                                field.Name, field.GetType().Name);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("The form is not an XFA form. AcroForm filling is not implemented in this example.");
                }

                // Directly patch the XFA datasets XML for image fields.
                // ImageValueBase64 appears to be a no-op in Spire 12.x — direct XML edit is required.
                if (form.XFAForm?.XmlDatasets != null && _pendingImageBase64.Count > 0)
                {
                    try
                    {
                        XmlNode xfaRoot = form.XFAForm.XmlDatasets;
                        XmlDocument xfaDoc = xfaRoot as XmlDocument ?? xfaRoot.OwnerDocument;
                        _logger.LogInformation("XFA datasets root: {Root}", xfaRoot.Name);
                        _logger.LogInformation("XFA datasets preview: {Xml}",
                            xfaRoot.OuterXml.Length > 400 ? xfaRoot.OuterXml[..400] : xfaRoot.OuterXml);

                        foreach (var kv in _pendingImageBase64)
                        {
                            // Field name pattern: topmostSubform[0].Page1[0].ImageField1[0]
                            // Strip array indices to get simple element name: ImageField1
                            string localName = System.Text.RegularExpressions.Regex.Replace(
                                kv.Key.Split('.')[^1], @"\[\d+\]", "");

                            var node = xfaRoot.SelectSingleNode($"//*[local-name()='{localName}']");
                            if (node != null)
                            {
                                node.InnerText = kv.Value;
                                var attr = node.Attributes["xfa:contentType"]
                                    ?? xfaDoc.CreateAttribute("xfa", "contentType", "http://www.xfa.org/schema/xfa-data/1.0/");
                                attr.Value = "image/png";
                                node.Attributes.SetNamedItem(attr);
                                _logger.LogInformation("XFA datasets: set image data on <{Node}> ({Len} chars).",
                                    localName, kv.Value.Length);
                            }
                            else
                            {
                                _logger.LogWarning("XFA datasets: node <{Node}> not found.", localName);
                            }
                        }
                    }
                    catch (Exception xmlEx)
                    {
                        _logger.LogWarning(xmlEx, "Direct XFA datasets image patch failed.");
                    }
                }

                // Save via a temp file (mirrors the working VISA2014 approach).
                // SaveToStream on XFA PDFs can strip the rendered image data; SaveToFile does not.
                string tempPdf = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
                try
                {
                    pdfdoc.SaveToFile(tempPdf);
                    using var fs = File.OpenRead(tempPdf);
                    fs.CopyTo(outputStream);
                }
                finally
                {
                    try { File.Delete(tempPdf); } catch { }
                }
                _logger.LogInformation("PDF form filling complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during PDF form filling.");
                throw;
            }
            finally
            {
                // Safe to dispose image streams only after the document has been saved
                foreach (var ms in streamsToDispose)
                {
                    ms.Dispose();
                }
            }
        }

        public void MergePdfs(Stream[] sources, Stream outputStream)
        {
            if (sources == null || sources.Length == 0)
            {
                _logger.LogWarning("No PDF sources provided for merging.");
                return;
            }

            try
            {
                // IMPORTANT: Do NOT use PdfDocument.MergeFiles() with XFA PDFs.
                // MergeFiles() reconstructs the XFA layer from the source streams,
                // discarding any flattening applied by FillForm(). The result always
                // shows "Please wait..." in non-Adobe viewers.
                //
                // FIX: Build the merged document by importing pages one-by-one from
                // each already-flattened source into a fresh PdfDocument. This copies
                // only the rendered page content (static layers) — the XFA XML is
                // never reconstructed, so the output renders correctly everywhere.
                var mergedDoc = new PdfDocument();

                foreach (var sourceStream in sources)
                {
                    sourceStream.Position = 0;
                    var sourceDoc = new PdfDocument();
                    sourceDoc.LoadFromStream(sourceStream);

                    _logger.LogDebug("Importing {PageCount} page(s) from source stream.", sourceDoc.Pages.Count);

                    for (int i = 0; i < sourceDoc.Pages.Count; i++)
                    {
                        mergedDoc.InsertPage(sourceDoc, i);
                    }
                }

                _logger.LogDebug("All pages imported. Total pages in merged document: {PageCount}.", mergedDoc.Pages.Count);
                mergedDoc.SaveToStream(outputStream, FileFormat.PDF);
                _logger.LogInformation("{Count} PDF streams merged successfully via page import.", sources.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during PDF merging.");
                throw;
            }
        }
    }
}