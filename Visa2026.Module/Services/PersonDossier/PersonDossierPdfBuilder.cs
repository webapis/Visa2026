using System;
using System.IO;
using System.Text;
using DevExpress.Drawing.Printing;
using DevExpress.Office;
using DevExpress.XtraRichEdit;
using Microsoft.Extensions.Logging;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Renders the director hand-over dossier document as PDF.
/// </summary>
/// <remarks>
/// Uses the same DevExpress <c>RichEditDocumentServer</c> engine that
/// <c>ApplicationWordReportOfficePreviewPdfConverter</c> uses for Word previews, but imports
/// generated HTML instead of a template file. That keeps the dossier layout in code next to the
/// read model, so no <c>.docx</c> seed has to be kept in sync when sections change.
/// </remarks>
public sealed class PersonDossierPdfBuilder
{
    private readonly ILogger<PersonDossierPdfBuilder> logger;

    public PersonDossierPdfBuilder(ILogger<PersonDossierPdfBuilder> logger)
    {
        this.logger = logger;
    }

    public bool TryBuildPdf(PersonDossierSnapshot snapshot, string? cultureName, out byte[]? content)
    {
        content = null;
        if (snapshot == null || snapshot.PersonId == Guid.Empty)
            return false;

        try
        {
            string html = PersonDossierDocumentHtmlBuilder.Build(snapshot, cultureName);

            using var server = new RichEditDocumentServer();
            using (var input = new MemoryStream(Encoding.UTF8.GetBytes(html), writable: false))
            {
                // Fully qualified: the DocumentFormat.OpenXml package makes the bare name resolve to a namespace.
                server.LoadDocument(input, DevExpress.XtraRichEdit.DocumentFormat.Html);
            }

            ApplyPageSetup(server);
            server.Options.Printing.EnablePageBackgroundOnPrint = true;

            using var output = new MemoryStream();
            server.ExportToPdf(output);
            content = output.ToArray();
            return content.Length > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Person dossier export: failed to render the dossier document for person {PersonId}.",
                snapshot.PersonId);
            return false;
        }
    }

    private const float PageMarginMillimeters = 12f;

    private static void ApplyPageSetup(RichEditDocumentServer server)
    {
        var document = server.Document;

        // Measurements below are then interpreted in millimeters.
        document.Unit = DocumentUnit.Millimeter;

        foreach (var section in document.Sections)
        {
            section.Page.PaperKind = DXPaperKind.A4;
            section.Page.Landscape = false;
            section.Margins.Left = PageMarginMillimeters;
            section.Margins.Right = PageMarginMillimeters;
            section.Margins.Top = PageMarginMillimeters;
            section.Margins.Bottom = PageMarginMillimeters;
        }
    }
}
