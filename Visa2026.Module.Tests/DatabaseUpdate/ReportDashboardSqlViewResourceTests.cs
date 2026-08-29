using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public sealed class ReportDashboardSqlViewResourceTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Load_BlankLeaf_ThrowsArgumentException(string leaf)
    {
        Assert.Throws<ArgumentException>(() => ReportDashboardSqlViewResource.Load(leaf));
    }

    [Fact]
    public void Load_NullLeaf_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ReportDashboardSqlViewResource.Load(null!));
    }

    [Fact]
    public void Load_MissingResource_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => ReportDashboardSqlViewResource.Load("does_not_exist.postgres.sql"));

        Assert.Contains("Visa2026.Module.SqlViews.does_not_exist.postgres.sql", ex.Message);
    }

    [Fact]
    public void Load_EmbeddedPostgresView_ReturnsNonEmptySql()
    {
        var sql = ReportDashboardSqlViewResource.Load("vw_rd_visa_by_period.postgres.sql");

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("CREATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vw_rd_visa_by_period", sql, StringComparison.OrdinalIgnoreCase);
    }
}
