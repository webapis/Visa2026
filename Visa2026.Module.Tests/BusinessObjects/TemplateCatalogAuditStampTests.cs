#nullable enable

using System;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class TemplateCatalogAuditStampTests
{
    [Fact]
    public void Touch_sets_created_once_and_refreshs_modified()
    {
        var template = new ApplicationProfileTemplate();
        TemplateCatalogAuditStamp.Touch(template, "Ali Enes");
        var created = template.CreatedOnUtc;
        Assert.NotNull(created);
        Assert.Equal("Ali Enes", template.CreatedByUserName);

        TemplateCatalogAuditStamp.Touch(template, "Serdar");
        Assert.Equal(created, template.CreatedOnUtc);
        Assert.Equal("Ali Enes", template.CreatedByUserName);
        Assert.Equal("Serdar", template.ModifiedByUserName);
        Assert.True(template.ModifiedOnUtc >= created);
    }

    [Fact]
    public void FormatQuietLine_prefers_modified_when_later()
    {
        var created = DateTime.SpecifyKind(new DateTime(2026, 8, 1, 8, 0, 0), DateTimeKind.Utc);
        var modified = DateTime.SpecifyKind(new DateTime(2026, 9, 2, 11, 30, 0), DateTimeKind.Utc);
        var line = TemplateCatalogAuditStamp.FormatQuietLine(created, "Ali Enes", modified, "Serdar");
        Assert.NotNull(line);
        Assert.Contains("Serdar", line, StringComparison.Ordinal);
        Assert.DoesNotContain("Ali Enes", line, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatQuietLine_uses_created_when_never_updated()
    {
        var created = DateTime.UtcNow;
        var line = TemplateCatalogAuditStamp.FormatQuietLine(created, "Ali Enes", created, "Ali Enes");
        Assert.Contains("Ali Enes", line, StringComparison.Ordinal);
    }
}