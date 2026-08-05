using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class WordReportDefinitionScopeHelperTests
{
    [Theory]
    [InlineData(UserReportBoType.Application, WordReportPackageScope.Application, true)]
    [InlineData(UserReportBoType.ApplicationItem, WordReportPackageScope.Application, false)]
    [InlineData(UserReportBoType.Person, WordReportPackageScope.Application, false)]
    [InlineData(UserReportBoType.ApplicationItem, WordReportPackageScope.ApplicationItem, true)]
    [InlineData(UserReportBoType.Person, WordReportPackageScope.ApplicationItem, true)]
    [InlineData(UserReportBoType.Application, WordReportPackageScope.ApplicationItem, false)]
    public void MatchesUserTemplateScope_AlignsRootBoWithPackageScope(
        UserReportBoType rootBoType,
        WordReportPackageScope scope,
        bool expected)
    {
        Assert.Equal(expected, WordReportDefinitionScopeHelper.MatchesUserTemplateScope(rootBoType, scope));
    }

    [Fact]
    public void MatchesUserTemplateScope_UnknownScope_IsFalse()
    {
        Assert.False(WordReportDefinitionScopeHelper.MatchesUserTemplateScope(
            UserReportBoType.Application,
            (WordReportPackageScope)999));
    }
}
