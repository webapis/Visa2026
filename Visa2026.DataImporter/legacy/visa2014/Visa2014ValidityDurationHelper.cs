using DevExpress.ExpressApp;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ValidityDurationHelper
{
    internal static readonly int[] CandidateDaySpans = [90, 180, 365];

    public static int ComputeDaySpan(DateTime startDate, DateTime expireDate) =>
        (expireDate.Date - startDate.Date).Days;

    public static int ClosestCandidateDaySpan(int actualDays) =>
        CandidateDaySpans
            .OrderBy(c => Math.Abs(c - actualDays))
            .ThenBy(c => c)
            .First();

    public static string LocalizationKeyForDaySpan(int numberOfDays) => numberOfDays switch
    {
        90 => "Month3",
        180 => "Month6",
        365 => "Year1",
        _ => numberOfDays.ToString(),
    };

    public static Guid ResolveClosestValidityDurationId(
        INonSecuredObjectSpaceFactory factory,
        DateTime startDate,
        DateTime expireDate)
    {
        var actualDays = ComputeDaySpan(startDate, expireDate);
        var targetDays = ClosestCandidateDaySpan(actualDays);

        using var objectSpace = factory.CreateNonSecuredObjectSpace(typeof(Bo.ValidityDuration));
        var durations = objectSpace.GetObjectsQuery<Bo.ValidityDuration>().ToList();
        var match = durations.FirstOrDefault(d => d.NumberOfDays == targetDays);
        if (match == null)
        {
            throw new InvalidOperationException(
                $"Could not resolve ValidityDuration with NumberOfDays={targetDays} — ensure lookup catalogs are seeded.");
        }

        return match.ID;
    }

    public static Guid ResolveClosestVisaPeriodId(
        INonSecuredObjectSpaceFactory factory,
        DateTime startDate,
        DateTime expireDate)
    {
        var actualDays = ComputeDaySpan(startDate, expireDate);
        var targetDays = ClosestCandidateDaySpan(actualDays);
        var localizationKey = LocalizationKeyForDaySpan(targetDays);

        using var objectSpace = factory.CreateNonSecuredObjectSpace(typeof(Bo.VisaPeriod));
        var periods = objectSpace.GetObjectsQuery<Bo.VisaPeriod>().ToList();
        var match = periods.FirstOrDefault(p =>
            string.Equals(p.LocalizationKey, localizationKey, StringComparison.OrdinalIgnoreCase));
        if (match == null)
        {
            match = periods.FirstOrDefault(p => p.IsDefault)
                ?? throw new InvalidOperationException(
                    $"Could not resolve VisaPeriod LocalizationKey={localizationKey} — ensure lookup catalogs are seeded.");
        }

        return match.ID;
    }

    public static Guid ResolveDefaultVisaCategoryId(INonSecuredObjectSpaceFactory factory)
    {
        using var objectSpace = factory.CreateNonSecuredObjectSpace(typeof(Bo.VisaCategory));
        var categories = objectSpace.GetObjectsQuery<Bo.VisaCategory>().ToList();
        var match = categories.FirstOrDefault(c => c.IsDefault)
            ?? categories.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Could not resolve default VisaCategory — ensure lookup catalogs are seeded.");
        return match.ID;
    }
}