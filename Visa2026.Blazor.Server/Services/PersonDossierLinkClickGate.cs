namespace Visa2026.Blazor.Server.Services;

using System.Threading;

/// <summary>
/// Short-lived flag set when a person dossier ListView link cell is clicked,
/// so row navigation can be suppressed without blocking other columns.
/// </summary>
public static class PersonDossierLinkClickGate
{
    private static int _pending;

    public static void MarkPending() => Interlocked.Increment(ref _pending);

    public static bool ConsumePending() => Interlocked.Exchange(ref _pending, 0) > 0;
}
