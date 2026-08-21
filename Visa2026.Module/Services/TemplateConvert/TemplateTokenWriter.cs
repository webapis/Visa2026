#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <inheritdoc cref="ITemplateTokenWriter"/>
public sealed class TemplateTokenWriter : ITemplateTokenWriter
{
    public TokenWriteResult Apply(TemplateTokenWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.SourceContent);

        if (request.SourceContent.Length == 0)
            throw new ArgumentException("Source document is empty.", nameof(request));

        return request.Format switch
        {
            TemplateSourceFormat.Docx => WordTemplateTokenWriter.Write(request.SourceContent, request.Substitutions, request.Loops),
            TemplateSourceFormat.Xlsx => ExcelTemplateTokenWriter.Write(request.SourceContent, request.Substitutions, request.Loops),
            _ => throw new NotSupportedException($"Unsupported template format '{request.Format}'."),
        };
    }
}
