using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// vw_rd_invitation_in_process must classify short periods (≤30 days or exactly 1 month)
/// so the short/long Process tabs filter correctly.
/// </summary>
public sealed class InvitationProcessShortVisaPeriodViewSqlTests
{
    [Fact]
    public void InvitationInProcessView_DefinesIsShortVisaPeriod_ForGunUpTo30AndAyEquals1()
    {
        var sql = LoadViewSql("vw_rd_invitation_in_process.postgres.sql");

        Assert.Contains("AS \"IsShortVisaPeriod\"", sql, StringComparison.Ordinal);
        Assert.Contains("= 'GUN'", sql, StringComparison.Ordinal);
        Assert.Contains("BETWEEN 1 AND 30", sql, StringComparison.Ordinal);
        Assert.Contains("= 'AY'", sql, StringComparison.Ordinal);
        Assert.Contains("PdfForm_Count\", 0) = 1", sql, StringComparison.Ordinal);
        Assert.Contains("THEN TRUE", sql, StringComparison.Ordinal);
        Assert.Contains("ELSE FALSE", sql, StringComparison.Ordinal);
    }

    private static string LoadViewSql(string leaf)
    {
        var assembly = Assembly.Load("Visa2026.Module");
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(leaf, StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrEmpty(name), "Embedded resource missing: " + leaf);

        using var stream = assembly.GetManifestResourceStream(name!);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
}
