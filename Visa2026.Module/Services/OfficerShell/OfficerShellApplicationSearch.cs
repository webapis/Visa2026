using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Staged / in-process queue search: token-AND over linked people names, passport
/// numbers, and the extra row fields (application number, profile, project, …).
/// </summary>
public static class OfficerShellApplicationSearch
{
    public static string BuildHaystack(ApplicationProfileInstance? application, params string?[] extra)
    {
        var parts = new List<string>();
        if (extra != null)
        {
            foreach (var value in extra)
                Add(parts, value);
        }

        if (application == null)
            return string.Join('\n', parts);

        foreach (var person in ApplicationRosterHelper.GetRosterPeople(application))
        {
            Add(parts, person.FirstName);
            Add(parts, person.MiddleName);
            Add(parts, person.LastName);
            Add(parts, person.FullName);
            if (person.Passports == null)
                continue;

            foreach (var passport in person.Passports)
                Add(parts, passport.PassportNumber);
        }

        if (application.Passports != null)
        {
            foreach (var passport in application.Passports)
                Add(parts, passport.PassportNumber);
        }

        return string.Join('\n', parts);
    }

    public static bool Matches(string? searchText, string? haystack)
    {
        var tokens = ReportDashboardCatalog.PersonSearchTokens(searchText);
        if (tokens.Length == 0)
            return true;

        var folded = PersonSearchTextNormalizer.Fold(haystack ?? string.Empty);
        foreach (var token in tokens)
        {
            if (!folded.Contains(token, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static void Add(List<string> parts, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parts.Add(value.Trim());
    }
}