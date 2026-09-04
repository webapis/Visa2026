using System;
using System.Collections.Generic;
using System.IO;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class PdfFormFillerImageFieldTests
{
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public void MapApplicationData_IncludesPersonPhotoBytes()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { PdfForm_Code = 2 },
        };
        var item = new ApplicationRosterMergeLine
        {
            SuppressPersonCurrentFieldSync = true,
            ApplicationProfileInstance = application,
            Person = new Person { FirstName = "Ali", LastName = "Yetkin", Photo = TinyPng },
        };
        var data = new Dictionary<string, object>();
        var mappings = PdfMappingHelper.FinalizeMappings(Array.Empty<PdfFormMappingDefinition>());

        PdfMappingHelper.MapApplicationData(data, application, item, objectSpace: null, logger: null, mappings);

        Assert.True(data.TryGetValue("topmostSubform[0].Page1[0].ImageField1[0]", out var photo));
        Assert.IsType<byte[]>(photo);
        Assert.Equal(TinyPng, photo);
    }

    [Fact]
    public void FromBytes_ReturnsPngDataUri()
    {
        var uri = PdfPersonPhotoDataUri.FromBytes(TinyPng);
        Assert.StartsWith("data:image/png;base64,", uri);
        Assert.Contains(Convert.ToBase64String(TinyPng), uri);
    }

    [Fact]
    public void FromBytes_Empty_ReturnsNull()
    {
        Assert.Null(PdfPersonPhotoDataUri.FromBytes(null));
        Assert.Null(PdfPersonPhotoDataUri.FromBytes([]));
    }

    [Fact]
    public void FillForm_AssignsImageField1InMemory()
    {
        var templatePath = ApplicationFilledFormPdfGenerator.ResolveTemplatePath(
            "Resources/Visa_Application_TM_QR_08.pdf",
            out var temporaryPath);
        Assert.False(string.IsNullOrWhiteSpace(templatePath));

        try
        {
            var logger = new CollectingLogger();
            var filler = new PdfFormFillerService(logger);
            var data = new Dictionary<string, object>
            {
                ["topmostSubform[0].Page1[0].ImageField1[0]"] = TinyPng,
            };

            using var output = new MemoryStream();
            filler.FillForm(templatePath!, output, data);
            Assert.True(output.Length > 1000);
            Assert.Contains(logger.Lines, line => line.Contains("ImageValueBase64 set", StringComparison.Ordinal));
            Assert.Contains(logger.Lines, line => line.Contains("XFA template: set <image>", StringComparison.Ordinal));
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryPath) && File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private sealed class CollectingLogger : Microsoft.Extensions.Logging.ILogger<PdfFormFillerService>
    {
        public List<string> Lines { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Noop.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            Lines.Add(formatter(state, exception));
        }

        private sealed class Noop : IDisposable
        {
            public static readonly Noop Instance = new();
            public void Dispose() { }
        }
    }
}
