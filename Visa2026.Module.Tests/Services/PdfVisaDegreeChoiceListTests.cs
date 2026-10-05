using System.Collections.Generic;
using System.IO;
using System.Xml;
using Spire.Pdf;
using Spire.Pdf.Widget;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class PdfVisaDegreeChoiceListTests
{
    [Fact]
    public void Disambiguate_KeepsSharedCodeOnSelectedVisaType()
    {
        var doc = new XmlDocument();
        doc.LoadXml("""
            <template>
              <field name="_25">
                <items>
                  <text>BS1 - ISEWURLIK</text>
                  <text>TR2 - USTASYR</text>
                  <text>WP - ISHCHI WIZA</text>
                </items>
                <items save="1">
                  <text>14</text>
                  <text>14</text>
                  <text>11</text>
                </items>
              </field>
            </template>
            """);

        Assert.True(PdfVisaDegreeChoiceList.Disambiguate(doc.DocumentElement, "BS1"));
        Assert.Equal("14", PdfVisaDegreeChoiceList.SaveCodeFor(doc.DocumentElement, "BS1"));
        Assert.NotEqual("14", PdfVisaDegreeChoiceList.SaveCodeFor(doc.DocumentElement, "TR2"));
        Assert.Equal("11", PdfVisaDegreeChoiceList.SaveCodeFor(doc.DocumentElement, "WP"));
    }

    [Fact]
    public void Disambiguate_Tr2KeepsSharedCodeWhenThatTypeIsSelected()
    {
        var doc = new XmlDocument();
        doc.LoadXml("""
            <template>
              <field name="_25">
                <items>
                  <text>BS1 - ISEWURLIK</text>
                  <text>TR2 - USTASYR</text>
                </items>
                <items save="1">
                  <text>14</text>
                  <text>14</text>
                </items>
              </field>
            </template>
            """);

        Assert.True(PdfVisaDegreeChoiceList.Disambiguate(doc.DocumentElement, "TR2"));
        Assert.Equal("14", PdfVisaDegreeChoiceList.SaveCodeFor(doc.DocumentElement, "TR2"));
        Assert.NotEqual("14", PdfVisaDegreeChoiceList.SaveCodeFor(doc.DocumentElement, "BS1"));
    }

    [Fact]
    public void MapApplicationData_RemembersVisaTypeLocalizationKey()
    {
        var application = new ApplicationProfileInstance
        {
            VisaType = new VisaType { LocalizationKey = "BS1", PdfForm_Code = 14 },
        };
        var item = new ApplicationRosterMergeLine
        {
            SuppressPersonCurrentFieldSync = true,
            ApplicationProfileInstance = application,
        };
        var data = new Dictionary<string, object>();

        PdfMappingHelper.MapApplicationData(data, application, item, objectSpace: null, logger: null, mappings: []);

        Assert.Equal("BS1", data[PdfVisaDegreeChoiceList.LocalizationKeyDataKey]);
    }

    [Fact]
    public void FillForm_PersistsBs1AsTheOnlyRowForCode14()
    {
        var templatePath = ApplicationFilledFormPdfGenerator.ResolveTemplatePath(
            "Resources/Visa_Application_TM_QR_08.pdf",
            out var temporaryPath);
        Assert.False(string.IsNullOrWhiteSpace(templatePath));

        try
        {
            var filler = new PdfFormFillerService(new CollectingLogger());
            var data = new Dictionary<string, object>
            {
                [PdfVisaDegreeChoiceList.FieldKey] = 14,
                [PdfVisaDegreeChoiceList.LocalizationKeyDataKey] = "BS1",
            };

            using var output = new MemoryStream();
            filler.FillForm(templatePath!, output, data);
            output.Position = 0;

            var pdf = new PdfDocument();
            pdf.LoadFromStream(output);
            var form = (PdfFormWidget)pdf.Form;
            var template = form.XFAForm.XmlTemplate;

            Assert.Equal("14", PdfVisaDegreeChoiceList.SaveCodeFor(template, "BS1"));
            Assert.NotEqual("14", PdfVisaDegreeChoiceList.SaveCodeFor(template, "TR2"));

            var value = form.XFAForm.XmlDatasets?.SelectSingleNode("//*[local-name()='_25']");
            Assert.Equal("14", value?.InnerText?.Trim());
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryPath) && File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private sealed class CollectingLogger : Microsoft.Extensions.Logging.ILogger<PdfFormFillerService>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
        }
    }
}
