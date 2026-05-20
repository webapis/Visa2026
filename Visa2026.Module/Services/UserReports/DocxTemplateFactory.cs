using System.IO;
using DocxTemplater;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Opens DocxTemplater templates for user Word reports (text merge; photos via <see cref="WordUserReportImageInjector"/>).</summary>
public static class DocxTemplateFactory
{
    public static DocxTemplate Open(Stream templateStream) => new(templateStream);
}
