using System;
using System.Collections.Generic;

namespace Visa2026.Module.Services;

/// <summary>
/// PdfFormMapping keys are full XFA paths. Spire may expose field.Name as the last node (_01 or _01[0]).
/// </summary>
internal static class PdfXfaFieldValueLookup
{
    public static bool TryGetValue(IReadOnlyDictionary<string, object> data, string fieldName, out object value)
    {
        value = null;
        if (data == null || string.IsNullOrWhiteSpace(fieldName))
            return false;

        if (data.TryGetValue(fieldName, out value) && value != null)
            return true;

        var fieldLocal = LocalName(fieldName);
        if (string.IsNullOrEmpty(fieldLocal))
            return false;

        foreach (var pair in data)
        {
            if (pair.Value == null || string.IsNullOrEmpty(pair.Key))
                continue;
            if (string.Equals(LocalName(pair.Key), fieldLocal, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    internal static string LocalName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var last = name;
        var dot = name.LastIndexOf('.');
        if (dot >= 0 && dot < name.Length - 1)
            last = name[(dot + 1)..];

        var bracket = last.IndexOf('[');
        return bracket >= 0 ? last[..bracket] : last;
    }
}