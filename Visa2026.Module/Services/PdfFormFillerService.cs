using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Visa2026.Module.Services.RuntimeLogging;
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
                _logger.LogErrorWithCode(
                    ApplicationRuntimeLogErrorCodes.PdfFillTemplateMissing,
                    "Template PDF file not found at {TemplatePath}",
                    templatePath);
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
                    _logger.LogErrorWithCode(
                        ApplicationRuntimeLogErrorCodes.PdfFillNoAcroForm,
                        "PDF document does not contain a form.");
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
                        if (_logger.IsEnabled(LogLevel.Debug)
                            && field.Name != null
                            && field.Name.IndexOf("Image", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _logger.LogDebug("XFA image-like field '{FieldName}' type={FieldType}.",
                                field.Name, field.GetType().Name);
                        }

                        if (PdfXfaFieldValueLookup.TryGetValue(data, field.Name, out object value) && value != null)
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
                                        _logger.LogErrorWithCode(
                                            ApplicationRuntimeLogErrorCodes.PdfFillFieldError,
                                            ex,
                                            "Image field '{FieldName}': Error preparing image data.",
                                            field.Name);
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
                                _logger.LogErrorWithCode(
                                    ApplicationRuntimeLogErrorCodes.PdfFillFieldError,
                                    ex,
                                    "Error setting field {FieldName} with value {FieldValue}",
                                    field.Name,
                                    value);
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

                // ImageEdit fields are often missing from XfaFields / not typed as XfaImageField.
                // Collect byte[] photos from the mapping dictionary so XML patches still run.
                // Foxit/Adobe paint the saved ImageField1; pdf.js does not — preview overlays
                // Person.Photo via PdfPersonPhotoDataUri.
                foreach (var pair in data)
                {
                    if (!TryGetPngBytes(pair.Value, out var pngBytes) || pngBytes.Length == 0)
                        continue;
                    var b64 = Convert.ToBase64String(pngBytes);
                    if (!_pendingImageBase64.ContainsKey(pair.Key))
                        _pendingImageBase64[pair.Key] = b64;
                }

                // Direct XML edit is required: ImageValueBase64 is a no-op in Spire 12.x.
                // pdf.js XFA preview also needs <value><image> on the template field (not datasets only).
                if (_pendingImageBase64.Count > 0 && form.XFAForm != null)
                {
                    PatchXfaImagePackets(form.XFAForm, _pendingImageBase64);
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
                _logger.LogErrorWithCode(
                    ApplicationRuntimeLogErrorCodes.PdfFillUnexpected,
                    ex,
                    "An unexpected error occurred during PDF form filling.");
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
                // IMPORTANT: Do NOT use this merge for filled XFA application forms (Visa_Application_TM_QR_08).
                // InsertPage copies only the static XFA placeholder ("Please wait…"), not filled field content.
                // Use raw FillForm output per line, or a ZIP of separate PDFs (see ApplicationFilledFormPdfGenerator).
                // PdfSharpCore merge (SupportingDocumentsPdfSharpHelper) is for scanned attachment PDFs only.
                //
                // Do NOT use PdfDocument.MergeFiles() with XFA PDFs either — it reconstructs the XFA layer.
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
                _logger.LogErrorWithCode(
                    ApplicationRuntimeLogErrorCodes.PdfFillUnexpected,
                    ex,
                    "An unexpected error occurred during PDF merging.");
                throw;
            }
        }

        private void PatchXfaImagePackets(XFAForm xfaForm, Dictionary<string, string> imagesByFieldName)
        {
            foreach (var kv in imagesByFieldName)
            {
                var localName = PdfXfaFieldValueLookup.LocalName(kv.Key);
                if (string.IsNullOrEmpty(localName))
                    continue;

                try
                {
                    xfaForm[localName] = kv.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "XFAForm indexer could not set '{Name}'.", localName);
                }

                PatchXfaDatasetsImage(xfaForm.XmlDatasets, localName, kv.Value);
                PatchXfaTemplateImage(xfaForm.XmlTemplate, localName, kv.Value);
            }
        }

        private void PatchXfaDatasetsImage(XmlNode datasetsRoot, string localName, string base64)
        {
            if (datasetsRoot == null || string.IsNullOrEmpty(localName))
                return;

            try
            {
                var doc = datasetsRoot as XmlDocument ?? datasetsRoot.OwnerDocument;
                if (doc == null)
                    return;

                var node = datasetsRoot.SelectSingleNode($"//*[local-name()='{localName}']");
                if (node == null)
                {
                    _logger.LogWarning("XFA datasets: node <{Node}> not found.", localName);
                    return;
                }

                node.InnerText = base64;
                var attr = node.Attributes["xfa:contentType"]
                    ?? doc.CreateAttribute("xfa", "contentType", "http://www.xfa.org/schema/xfa-data/1.0/");
                attr.Value = "image/png";
                node.Attributes.SetNamedItem(attr);
                _logger.LogInformation("XFA datasets: set image data on <{Node}> ({Len} chars).",
                    localName, base64.Length);
            }
            catch (Exception xmlEx)
            {
                _logger.LogWarning(xmlEx, "XFA datasets image patch failed for {Node}.", localName);
            }
        }

        private void PatchXfaTemplateImage(XmlNode templateRoot, string localName, string base64)
        {
            if (templateRoot == null || string.IsNullOrEmpty(localName))
                return;

            try
            {
                var field = templateRoot.SelectSingleNode($"//*[local-name()='field' and @name='{localName}']");
                if (field == null)
                {
                    _logger.LogWarning("XFA template: field '{Name}' not found.", localName);
                    return;
                }

                var doc = field.OwnerDocument;
                if (doc == null)
                    return;

                var ns = field.NamespaceURI;
                XmlNode valueNode = null;
                XmlNode imageNode = null;
                foreach (XmlNode child in field.ChildNodes)
                {
                    if (child.LocalName == "value")
                        valueNode = child;
                }

                if (valueNode != null)
                {
                    foreach (XmlNode child in valueNode.ChildNodes)
                    {
                        if (child.LocalName == "image")
                            imageNode = child;
                    }
                }

                if (valueNode == null)
                {
                    valueNode = string.IsNullOrEmpty(ns)
                        ? doc.CreateElement("value")
                        : doc.CreateElement("value", ns);
                    field.AppendChild(valueNode);
                }

                if (imageNode == null)
                {
                    imageNode = string.IsNullOrEmpty(ns)
                        ? doc.CreateElement("image")
                        : doc.CreateElement("image", ns);
                    valueNode.AppendChild(imageNode);
                }

                var contentType = imageNode.Attributes["contentType"] ?? doc.CreateAttribute("contentType");
                contentType.Value = "image/png";
                imageNode.Attributes.SetNamedItem(contentType);

                var encoding = imageNode.Attributes["transferEncoding"] ?? doc.CreateAttribute("transferEncoding");
                encoding.Value = "base64";
                imageNode.Attributes.SetNamedItem(encoding);

                if (imageNode.Attributes["href"] != null)
                    imageNode.Attributes.RemoveNamedItem("href");

                imageNode.InnerText = base64;
                _logger.LogInformation("XFA template: set <image> on field '{Name}' ({Len} chars).",
                    localName, base64.Length);
            }
            catch (Exception xmlEx)
            {
                _logger.LogWarning(xmlEx, "XFA template image patch failed for {Name}.", localName);
            }
        }

        private static bool TryGetPngBytes(object value, out byte[] pngBytes)
        {
            pngBytes = null;
            if (value is byte[] rawBytes && rawBytes.Length > 0)
            {
                try
                {
                    using var ms = new MemoryStream(rawBytes);
                    using var img = Image.FromStream(ms);
                    using var tmp = new MemoryStream();
                    img.Save(tmp, ImageFormat.Png);
                    pngBytes = tmp.ToArray();
                    return pngBytes.Length > 0;
                }
                catch
                {
                    pngBytes = rawBytes;
                    return true;
                }
            }

            if (value is Image imageObj)
            {
                using var tmp = new MemoryStream();
                imageObj.Save(tmp, ImageFormat.Png);
                pngBytes = tmp.ToArray();
                return pngBytes.Length > 0;
            }

            return false;
        }
    }
}