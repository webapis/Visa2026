#nullable enable

using Microsoft.Extensions.DependencyInjection;
using PdfSharpCore.Pdf;
using Visa2026.Module.Services.TemplateScan;

namespace Visa2026.Module.Tests.TemplateScan;

internal static class ScanTestImageFactory
{
    private static readonly byte[] OneByOneWhitePng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82,
    ];

    public static byte[] CreatePngWithDimensions(int width, int height)
    {
        var png = (byte[])OneByOneWhitePng.Clone();
        WriteIntBigEndian(png, 16, width);
        WriteIntBigEndian(png, 20, height);
        return png;
    }

    public static byte[] CreatePdf(int pageCount)
    {
        using var document = new PdfDocument();
        for (var i = 0; i < pageCount; i++)
            document.AddPage();

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static void WriteIntBigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)((value >> 24) & 0xFF);
        buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 3] = (byte)(value & 0xFF);
    }
}

internal static class ScanTestServiceFactory
{
    public static (IScanInputNormalizer Normalizer, IScanSuitabilityEvaluator Suitability, IScanIngestService Ingest, IScanFieldPlanService FieldPlan) Create(
        int maxPdfPages = 5)
    {
        var services = new ServiceCollection();
        services.AddTemplateScan();
        services.Configure<TemplateAiScanOptions>(o => o.MaxPdfPages = maxPdfPages);
        var provider = services.BuildServiceProvider();
        return (
            provider.GetRequiredService<IScanInputNormalizer>(),
            provider.GetRequiredService<IScanSuitabilityEvaluator>(),
            provider.GetRequiredService<IScanIngestService>(),
            provider.GetRequiredService<IScanFieldPlanService>());
    }
}
