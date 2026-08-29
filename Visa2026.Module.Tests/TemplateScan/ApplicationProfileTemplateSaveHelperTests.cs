#nullable enable

using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ApplicationProfileTemplateSaveHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Save_rejects_blank_template_name(string? name)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ApplicationProfileTemplateSaveHelper.Save(new ApplicationProfileTemplateSaveRequest
            {
                ObjectSpace = null!,
                Profile = null!,
                TemplateName = name!,
                DataScope = default,
                CatalogScope = default,
                Content = [1],
            }));

        Assert.Contains("template name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
