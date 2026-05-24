using System;
using System.Collections.Generic;

namespace Visa2026.Module.BusinessObjects
{
    internal static class OrganizationPassportLineHelper
    {
        /// <summary>Borçnama-style one line: number, authority, issue date with year suffix.</summary>
        public static string Format(string? passportNumber, string? authority, DateTime? issueDate)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(passportNumber))
                parts.Add(passportNumber.Trim());
            if (!string.IsNullOrWhiteSpace(authority))
                parts.Add(authority.Trim());
            if (issueDate is { } d && d != default)
                parts.Add($"{d:dd.MM.yyyy}ý.");
            return string.Join(", ", parts);
        }
    }
}
