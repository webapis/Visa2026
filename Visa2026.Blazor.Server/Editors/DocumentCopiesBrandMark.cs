namespace Visa2026.Blazor.Server.Editors;

/// <summary>
/// Dedicated Document copies brand mark (Prototype C: stacked pages + paperclip).
/// Shared across Person / ApplicationItem / Header entry points and slot chrome.
/// </summary>
internal static class DocumentCopiesBrandMark
{
    public const string ImageName = "DocumentCopies";

    /// <summary>Inline SVG for Blazor buttons (uses currentColor).</summary>
    public const string SvgMarkup =
        """<svg viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M9.2 5.2h7.2a1.6 1.6 0 0 1 1.6 1.6v11.4a1.6 1.6 0 0 1-1.6 1.6H9.2" stroke="currentColor" stroke-width="1.55" stroke-linecap="round" stroke-linejoin="round"/><path d="M7 4h7.4a1.6 1.6 0 0 1 1.6 1.6v11.6a1.6 1.6 0 0 1-1.6 1.6H7A1.6 1.6 0 0 1 5.4 17.2V5.6A1.6 1.6 0 0 1 7 4z" stroke="currentColor" stroke-width="1.55" stroke-linecap="round" stroke-linejoin="round"/><path d="M7.8 9.2h5.2M7.8 12h5.2M7.8 14.8h3.4" stroke="currentColor" stroke-width="1.35" stroke-linecap="round"/><path d="M4.4 10.2v4.1a2.15 2.15 0 0 0 4.3 0V8.55a1.45 1.45 0 0 0-2.9 0v4.95a.75.75 0 0 0 1.5 0V9.5" stroke="currentColor" stroke-width="1.55" stroke-linecap="round" stroke-linejoin="round"/></svg>""";
}
