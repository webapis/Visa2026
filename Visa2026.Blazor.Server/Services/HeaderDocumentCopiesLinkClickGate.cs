namespace Visa2026.Blazor.Server.Services;

public static class HeaderDocumentCopiesLinkClickGate
{
    private static int pending;

    public static void MarkPending() => Interlocked.Exchange(ref pending, 1);

    public static bool ConsumePending() => Interlocked.Exchange(ref pending, 0) == 1;
}
