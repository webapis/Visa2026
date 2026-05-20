using System.Reflection;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Shared placeholder key / merge value helpers for Word user reports.</summary>
internal static class UserReportPlaceholderBindingHelper
{
    /// <summary>Removes DocxTemplater formatter suffix (e.g. <c>Person_Photo:img(w:35mm)</c> → <c>Person_Photo</c>).</summary>
    public static string StripFormatterSuffix(string placeholderPath)
    {
        if (string.IsNullOrWhiteSpace(placeholderPath))
            return placeholderPath ?? string.Empty;

        var idx = placeholderPath.IndexOf(':');
        return idx > 0 ? placeholderPath[..idx] : placeholderPath;
    }

    /// <summary>Coerces values for <see cref="DocxTemplater.DocxTemplate.BindModel"/> (photos stay <see cref="byte[]"/>, not text).</summary>
    public static object CoerceMergeValue(object? value, string propertyPath)
    {
        var bindPath = StripFormatterSuffix(propertyPath);
        if (TryGetExpectedByteArrayType(bindPath, out _))
        {
            if (value is not byte[] bytes || bytes.Length == 0)
                return null!;
            return bytes;
        }

        return value ?? string.Empty;
    }

    private static bool TryGetExpectedByteArrayType(string propertyPath, out Type? declaringType)
    {
        declaringType = null;
        if (string.IsNullOrWhiteSpace(propertyPath))
            return false;

        var prop = typeof(BusinessObjects.ApplicationItem).GetProperty(
            propertyPath,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop?.PropertyType == typeof(byte[]))
        {
            declaringType = typeof(BusinessObjects.ApplicationItem);
            return true;
        }

        prop = typeof(BusinessObjects.Application).GetProperty(
            propertyPath,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop?.PropertyType == typeof(byte[]))
        {
            declaringType = typeof(BusinessObjects.Application);
            return true;
        }

        return false;
    }
}
