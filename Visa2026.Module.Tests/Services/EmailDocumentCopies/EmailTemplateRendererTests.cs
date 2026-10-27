using System;
using Visa2026.Module.Services.EmailDocumentCopies;
using Xunit;

namespace Visa2026.Module.Tests.Services.EmailDocumentCopies;

public class EmailTemplateRendererTests
{
    [Fact]
    public void Render_ReplacesKnownPlaceholders_CaseInsensitive()
    {
        var context = new EmailTemplateContext
        {
            PersonNames = new[] { "Ada Lovelace", "Grace Hopper" },
            DocumentLines = new[] { "Ada Lovelace: Passport A", "Grace Hopper: Visa B" },
            OfficerName = "Officer Kim",
            Date = new DateTime(2026, 10, 27)
        };

        var rendered = EmailTemplateRenderer.Render(
            "To {personnames} ({PERSONCOUNT})\n{documentlist}\n— {officername} on {date}",
            context);

        Assert.Contains("To Ada Lovelace, Grace Hopper (2)", rendered, StringComparison.Ordinal);
        Assert.Contains("- Ada Lovelace: Passport A", rendered, StringComparison.Ordinal);
        Assert.Contains("- Grace Hopper: Visa B", rendered, StringComparison.Ordinal);
        Assert.Contains("— Officer Kim on 27.10.2026", rendered, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Render_EmptyText_ReturnsEmpty(string? text)
    {
        Assert.Equal(string.Empty, EmailTemplateRenderer.Render(text, new EmailTemplateContext()));
    }
}
