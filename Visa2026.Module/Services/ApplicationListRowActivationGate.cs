namespace Visa2026.Module.Services;

/// <summary>
/// Set when an officer clicks a link inside an application list row
/// (the progress-step file preview). The row activation that would open
/// the case is ignored for a short moment so only the preview opens.
/// </summary>
public static class ApplicationListRowActivationGate
{
    private static long suppressUntilUtcTicks;

    public static void SuppressBriefly() =>
        Volatile.Write(ref suppressUntilUtcTicks, DateTime.UtcNow.AddMilliseconds(700).Ticks);

    public static bool ConsumeIfPending()
    {
        var until = Volatile.Read(ref suppressUntilUtcTicks);
        if (until == 0 || DateTime.UtcNow.Ticks > until)
            return false;

        Volatile.Write(ref suppressUntilUtcTicks, 0);
        return true;
    }
}
