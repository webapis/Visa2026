using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract guards for per-user Fluent theme preference columns on PermissionPolicyUser.
/// </summary>
public class ApplicationUserThemePreferenceSchemaSqlTests
{
    [Fact]
    public void EnsurePreferredThemeColumns_GuardMissingTableAndExistingColumns()
    {
        Assert.Contains("PermissionPolicyUser", ApplicationUserThemePreferenceSchemaSql.EnsurePreferredThemeCaptionColumnSql, StringComparison.Ordinal);
        Assert.Contains("PreferredThemeCaption", ApplicationUserThemePreferenceSchemaSql.EnsurePreferredThemeCaptionColumnSql, StringComparison.Ordinal);
        Assert.Contains("COL_LENGTH", ApplicationUserThemePreferenceSchemaSql.EnsurePreferredThemeCaptionColumnSql, StringComparison.Ordinal);

        Assert.Contains("PreferredThemeMode", ApplicationUserThemePreferenceSchemaSql.EnsurePreferredThemeModeColumnSql, StringComparison.Ordinal);
        Assert.Contains("PreferredSizeMode", ApplicationUserThemePreferenceSchemaSql.EnsurePreferredSizeModeColumnSql, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        ApplicationUserThemePreferenceSchemaSql.ApplyIfMissing(null!);
        ApplicationUserThemePreferenceSchemaSql.ApplyIfMissing("");
        ApplicationUserThemePreferenceSchemaSql.ApplyIfMissing("   ");
    }
}
