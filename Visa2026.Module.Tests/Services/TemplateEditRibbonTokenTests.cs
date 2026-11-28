using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class TemplateEditRibbonTokenTests
{
    [Fact]
    public void Round_trip_keeps_the_template_id()
    {
        var templateId = Guid.Parse("53033767-9e12-4c00-904a-9f80cdd382a8");
        var token = TemplateEditRibbonToken.Issue(
            "test-secret-value",
            templateId,
            "Admin",
            DateTimeOffset.UtcNow.AddHours(1));

        var ok = TemplateEditRibbonToken.TryValidate(
            "test-secret-value",
            token,
            DateTimeOffset.UtcNow,
            out var readId,
            out var userName);

        Assert.True(ok);
        Assert.Equal(templateId, readId);
        Assert.Equal("Admin", userName);
    }

    [Fact]
    public void Expired_or_tampered_token_is_rejected()
    {
        var templateId = Guid.NewGuid();
        var token = TemplateEditRibbonToken.Issue(
            "test-secret-value",
            templateId,
            "Admin",
            DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.False(TemplateEditRibbonToken.TryValidate(
            "test-secret-value",
            token,
            DateTimeOffset.UtcNow,
            out _,
            out _));

        var fresh = TemplateEditRibbonToken.Issue(
            "test-secret-value",
            templateId,
            "Admin",
            DateTimeOffset.UtcNow.AddHours(1));
        var broken = fresh.Substring(0, fresh.Length - 2) + "aa";
        Assert.False(TemplateEditRibbonToken.TryValidate(
            "test-secret-value",
            broken,
            DateTimeOffset.UtcNow,
            out _,
            out _));
    }

    [Fact]
    public void Host_token_is_not_limited_to_one_template()
    {
        var token = TemplateEditRibbonToken.IssueHost(
            "test-secret-value",
            "Admin",
            DateTimeOffset.UtcNow.AddHours(1));

        Assert.True(TemplateEditRibbonToken.TryValidateHost(
            "test-secret-value",
            token,
            DateTimeOffset.UtcNow,
            out var userName));
        Assert.Equal("Admin", userName);
        Assert.False(TemplateEditRibbonToken.TryValidate(
            "test-secret-value",
            token,
            DateTimeOffset.UtcNow,
            out _,
            out _));
    }
}
