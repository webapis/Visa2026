using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ReportDashboardSqlViewResourceTests
{
    /// <summary>
    /// A leaf missing from the csproj EmbeddedResource list only fails on a database that needs
    /// healing, so assert every leaf the host-start heal can execute resolves to SQL.
    /// </summary>
    [Fact]
    public void Load_EveryHealResourceLeaf_ReturnsSql()
    {
        var leaves = CollectResourceLeaves(typeof(ReportDashboardPostgresViewsHealSql));

        Assert.NotEmpty(leaves);
        foreach (var leaf in leaves)
        {
            var sql = ReportDashboardSqlViewResource.Load(leaf);
            Assert.False(string.IsNullOrWhiteSpace(sql), leaf + " resolved to empty SQL.");
        }
    }

    private static IReadOnlyList<string> CollectResourceLeaves(Type type)
    {
        var leaves = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var field in type.GetFields(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Collect(field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null), leaves);
        }

        return leaves.ToList();
    }

    private static void Collect(object? value, ISet<string> leaves)
    {
        switch (value)
        {
            case string text:
                if (text.EndsWith(".postgres.sql", StringComparison.Ordinal))
                    leaves.Add(text);
                break;
            case ITuple tuple:
                for (var i = 0; i < tuple.Length; i++)
                    Collect(tuple[i], leaves);
                break;
            case IEnumerable items:
                foreach (var item in items)
                    Collect(item, leaves);
                break;
        }
    }
}