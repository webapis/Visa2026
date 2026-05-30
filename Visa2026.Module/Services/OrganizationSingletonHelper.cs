using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.Services;

/// <summary>
/// Resolves organization singleton BOs (one row per deployment). Used by report helpers and
/// <see cref="BusinessObjects.CompanyProfile"/> / signatory / representative / numbering types.
/// </summary>
public static class OrganizationSingletonHelper
{
    public static T? TryGet<T>(
        IObjectSpace objectSpace,
        Func<T, string?> keySelector,
        Func<IReadOnlyList<T>, T>? tieBreaker = null) where T : class
    {
        var all = objectSpace.GetObjectsQuery<T>().ToList();
        var populated = all.Where(x => !string.IsNullOrWhiteSpace(keySelector(x))).ToList();

        if (populated.Count == 1)
            return populated[0];

        if (populated.Count > 1)
        {
            var chosen = tieBreaker?.Invoke(populated)
                ?? populated.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase).First();
            Tracing.Tracer.LogWarning(
                $"OrganizationSingletonHelper: multiple {typeof(T).Name} rows ({populated.Count}); "
                + $"using '{keySelector(chosen)}'. Run DB update to prune duplicates.");
            return chosen;
        }

        return all.Count == 1 ? all[0] : null;
    }

    /// <summary>Removes extra rows so at most one <typeparamref name="T"/> remains; returns rows deleted.</summary>
    public static int CollapseToSingleRow<T>(
        IObjectSpace objectSpace,
        Func<T, string?> keySelector,
        Func<IReadOnlyList<T>, T>? chooseKeeper = null) where T : class
    {
        var all = objectSpace.GetObjectsQuery<T>().ToList();
        if (all.Count <= 1)
            return 0;

        var populated = all.Where(x => !string.IsNullOrWhiteSpace(keySelector(x))).ToList();
        T keeper = chooseKeeper?.Invoke(populated.Count > 0 ? populated : all)
            ?? (populated.Count > 0
                ? populated.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase).First()
                : all[0]);

        int removed = 0;
        foreach (var row in all)
        {
            if (ReferenceEquals(row, keeper))
                continue;
            objectSpace.Delete(row);
            removed++;
        }

        return removed;
    }
}
