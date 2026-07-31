namespace Visa2026.Blazor.Server.Editors;

using Visa2026.Module.Services.WordReports;

/// <summary>
/// Word / Excel format icons for Resminamalar flat catalog cards (Document Copies nav language).
/// </summary>
internal static class ResminamalarCatalogFormatIcons
{
    public static string CssClass(ApplicationWordReportPackageEntryKind kind) =>
        kind == ApplicationWordReportPackageEntryKind.UserExcel
            ? "resminamalar-catalog__nav-icon--excel"
            : "resminamalar-catalog__nav-icon--word";

    public static string Svg(ApplicationWordReportPackageEntryKind kind) =>
        kind == ApplicationWordReportPackageEntryKind.UserExcel
            ? """<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="4" y="3" width="16" height="18" rx="2"/><path d="M4 9h16M4 15h16M10 3v18"/></svg>"""
            : """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 3h7l5 5v13a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z"/><path d="M14 3v5h5"/><path d="M9 13h6M9 17h4"/></svg>""";
}