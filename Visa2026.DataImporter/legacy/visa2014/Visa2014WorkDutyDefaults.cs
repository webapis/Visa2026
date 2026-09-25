namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// VISA2015 has no WorkDuty table. Import stores Description "Ýok" so an officer
/// can replace it on the case. Existing roster links are left as the officer left them.
/// </summary>
internal static class Visa2014WorkDutyDefaults
{
    internal const string Description = "\u00DDok";

    internal static bool ShouldCreate(bool isEmployee, bool profileShowsWorkDuty, bool alreadyLinked) =>
        isEmployee && profileShowsWorkDuty && !alreadyLinked;
}
