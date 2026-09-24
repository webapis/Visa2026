using System;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Seretmezlik letter numbers share the company outgoing-letter counter with
/// <see cref="ApplicationProfileInstance.ApplicationNumber"/> (same prefix / year / month scope).
/// </summary>
public static class ApplicationProfileInstanceExclusionNumbering
{
    /// <summary>Highest exclusion sequence in scope (DB + unsaved in <paramref name="objectSpace"/>).</summary>
    public static int MaxExclusionSequence(
        IObjectSpace? objectSpace,
        string? prefix,
        int year,
        int month,
        bool scopeByYear,
        bool scopeByMonth)
    {
        if (objectSpace == null)
            return 0;

        var query = objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusion>()
            .Where(e => e.AppNumberPrefix == prefix);
        if (scopeByYear || scopeByMonth) query = query.Where(e => e.Year == year);
        if (scopeByMonth) query = query.Where(e => e.Month == month);

        var maxDb = query
            .Select(e => e.SequenceNumber)
            .ToList()
            .Select(ParseSequence)
            .DefaultIfEmpty(0)
            .Max();

        var maxLocal = 0;
        if (objectSpace is BaseObjectSpace baseObjectSpace)
        {
            maxLocal = baseObjectSpace.ModifiedObjects.OfType<ApplicationProfileInstanceExclusion>()
                .Where(e => !baseObjectSpace.IsObjectToDelete(e)
                    && e.AppNumberPrefix == prefix
                    && (!(scopeByYear || scopeByMonth) || e.Year == year)
                    && (!scopeByMonth || e.Month == month))
                .Select(e => ParseSequence(e.SequenceNumber))
                .DefaultIfEmpty(0)
                .Max();
        }

        return Math.Max(maxDb, maxLocal);
    }

    /// <summary>
    /// Assigns prefix, year/month, sequence and <see cref="ApplicationProfileInstanceExclusion.LetterNumber"/>
    /// from <c>ApplicationNumberingProfile</c>. Call once, before the first commit.
    /// </summary>
    public static void Allocate(IObjectSpace objectSpace, ApplicationProfileInstanceExclusion exclusion)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(exclusion);

        var numbering = ApplicationProfileInstance.GetNumberingConfiguration(objectSpace);
        var fmt = numbering.Format;
        var scopeByYear = string.IsNullOrEmpty(fmt) || fmt.Contains("{YEAR}") || fmt.Contains("{YEAR2}");
        var scopeByMonth = !string.IsNullOrEmpty(fmt) && (fmt.Contains("{MONTH}") || fmt.Contains("{MONTH2}"));

        var letterDate = exclusion.LetterDate == default ? DateTime.Today : exclusion.LetterDate;
        exclusion.Year = letterDate.Year;
        exclusion.Month = letterDate.Month;
        exclusion.AppNumberPrefix = numbering.Prefix;

        var instanceQuery = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.AppNumberPrefix == exclusion.AppNumberPrefix);
        if (scopeByYear || scopeByMonth) instanceQuery = instanceQuery.Where(a => a.Year == exclusion.Year);
        if (scopeByMonth) instanceQuery = instanceQuery.Where(a => a.Month == exclusion.Month);
        var maxInstance = instanceQuery
            .Select(a => a.ApplicationNumber)
            .ToList()
            .Select(ParseSequence)
            .DefaultIfEmpty(0)
            .Max();

        var maxExclusion = MaxExclusionSequence(
            objectSpace, exclusion.AppNumberPrefix, exclusion.Year, exclusion.Month, scopeByYear, scopeByMonth);

        var next = Math.Max(Math.Max(maxInstance, maxExclusion), numbering.Seed) + 1;
        exclusion.SequenceNumber = next.ToString($"D{numbering.Padding}");
        exclusion.LetterNumber = ApplicationProfileInstance.BuildFullNumber(
            numbering.Format,
            exclusion.AppNumberPrefix ?? string.Empty,
            exclusion.Year,
            exclusion.Month,
            exclusion.SequenceNumber);
    }

    private static int ParseSequence(string? value) =>
        int.TryParse(value, out var n) ? n : 0;
}
