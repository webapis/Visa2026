using Microsoft.Extensions.Logging;
using Spire.Pdf;
using Spire.Pdf.Fields;
using Spire.Pdf.Widget;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

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

                if (form.XFAForm != null)
                {
                    List<XfaField> loFields = form.XFAForm.XfaFields;
                    _logger.LogDebug("XFA form detected. Total fields found: {FieldCount}. Data keys provided: {DataCount}.",
                        loFields.Count, data.Count);
                    _logger.LogDebug("XFA field names in template: [{FieldNames}]",
                        string.Join(", ", loFields.ConvertAll(f => $"{f.Name}({f.GetType().Name})")));

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
                                    textField.Value = value.ToString();
                                }
                                else if (field is XfaDateTimeField dateTimeField && value is DateTime dt)
                                {
                                    dateTimeField.Value = dt.ToString("dd.MM.yyyy");
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
                                    _logger.LogDebug("Image field '{FieldName}' found. Value type: {ValueType}.",
                                        field.Name, value?.GetType().FullName ?? "null");

                                    // FIX: Convert everything to byte[] first, then assign via a
                                    // long-lived MemoryStream. Do NOT use `using` here — the stream
                                    // must remain open until pdfdoc.SaveToStream() completes.
                                    byte[] imageBytes = null;

                                    if (value is byte[] rawBytes)
                                    {
                                        imageBytes = rawBytes;
                                        _logger.LogDebug("Image field '{FieldName}': using raw byte[] payload, length={Length}.",
                                            field.Name, imageBytes.Length);
                                    }
                                    else if (value is Image imageObj)
                                    {
                                        _logger.LogDebug("Image field '{FieldName}': converting Image object to PNG bytes. " +
                                            "Size={Width}x{Height}, PixelFormat={PixelFormat}.",
                                            field.Name, imageObj.Width, imageObj.Height, imageObj.PixelFormat);

                                        // Convert Image -> PNG bytes
                                        using (var tmp = new MemoryStream())
                                        {
                                            imageObj.Save(tmp, ImageFormat.Png);
                                            imageBytes = tmp.ToArray();
                                        }

                                        _logger.LogDebug("Image field '{FieldName}': PNG conversion produced {Length} bytes.",
                                            field.Name, imageBytes.Length);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Image field '{FieldName}': unsupported value type '{ValueType}'. " +
                                            "Expected byte[] or System.Drawing.Image.",
                                            field.Name, value?.GetType().FullName ?? "null");
                                    }

                                    if (imageBytes != null && imageBytes.Length > 0)
                                    {
                                        // Keep this stream alive until after Save
                                        var imgStream = new MemoryStream(imageBytes);
                                        streamsToDispose.Add(imgStream);

                                        try
                                        {
                                            var loadedImage = Image.FromStream(imgStream);
                                            imageField.Image = loadedImage;
                                            _logger.LogDebug("Image field '{FieldName}': Image.FromStream succeeded. " +
                                                "Resolved size={Width}x{Height}. Stream position={Position}/{Length}.",
                                                field.Name, loadedImage.Width, loadedImage.Height,
                                                imgStream.Position, imgStream.Length);
                                        }
                                        catch (Exception imgEx)
                                        {
                                            _logger.LogError(imgEx, "Image field '{FieldName}': Image.FromStream failed. " +
                                                "The byte payload may be corrupt or an unsupported format. " +
                                                "First 8 bytes (hex): {Header}.",
                                                field.Name,
                                                imageBytes.Length >= 8
                                                    ? BitConverter.ToString(imageBytes, 0, 8)
                                                    : BitConverter.ToString(imageBytes));
                                            throw;
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

                // Flatten the form. This converts the interactive XFA fields into static
                // content, making the PDF viewable in all readers, not just Adobe Reader.
                // This is crucial for preventing the "Please wait..." message in browser viewers.
                _logger.LogDebug("Flattening PDF form to ensure universal compatibility.");
                form.IsFlatten = true;

                _logger.LogDebug("Calling SaveToStream. Output stream: CanWrite={CanWrite}, Position={Position}.",
                    outputStream.CanWrite, outputStream.CanSeek ? outputStream.Position : -1);
                pdfdoc.SaveToStream(outputStream, FileFormat.PDF);
                _logger.LogInformation("PDF form filled successfully.");
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