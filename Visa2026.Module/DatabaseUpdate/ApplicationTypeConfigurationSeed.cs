using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Code-first <see cref="BusinessObjects.ApplicationType"/> configuration.
/// Regenerate <see cref="Rows"/> from LOOKUPS.md:
/// <c>scripts/local/Generate-ApplicationTypeConfigurationSeed.ps1</c>
/// </summary>
internal static partial class ApplicationTypeConfigurationSeed
{
    private static readonly Lazy<ApplicationTypeConfigurationRow[]> RowsLazy =
        new(CreateRows, isThreadSafe: true);

    private static readonly Lazy<Dictionary<string, ApplicationTypeConfigurationRow>> ByNameLazy =
        new(() => Rows.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase), isThreadSafe: true);

    public static IReadOnlyList<ApplicationTypeConfigurationRow> Rows => RowsLazy.Value;

    public static bool TryGetByName(string? name, out ApplicationTypeConfigurationRow row) =>
        ByNameLazy.Value.TryGetValue(name ?? "", out row!);
}
