using Microsoft.Extensions.Logging;
using Spire.Pdf;
using Spire.Pdf.Fields;
using Spire.Pdf.Widget;
using System;
using System.Collections.Generic;
using System.Drawing;
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
                    foreach (var field in loFields)
                    {
                        if (data.TryGetValue(field.Name, out object value) && value != null)
                        {
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
                                    if (value is Image img)
                                    {
                                        imageField.Image = img;
                                    }
                                    else if (value is byte[] imgBytes)
                                    {
                                        using (var ms = new MemoryStream(imgBytes))
                                        {
                                            imageField.Image = Image.FromStream(ms);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error setting field {FieldName} with value {FieldValue}", field.Name, value);
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("The form is not an XFA form. AcroForm filling is not implemented in this example.");
                }
                
                pdfdoc.SaveToStream(outputStream, FileFormat.PDF);
                _logger.LogInformation("PDF form filled successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during PDF form filling.");
                throw;
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
                PdfDocumentBase mergedDoc = PdfDocument.MergeFiles(sources);
                mergedDoc.Save(outputStream, FileFormat.PDF);
                _logger.LogInformation("{Count} PDF streams merged successfully.", sources.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during PDF merging.");
                throw;
            }
        }
    }
}