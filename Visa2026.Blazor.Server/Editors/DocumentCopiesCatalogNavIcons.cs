namespace Visa2026.Blazor.Server.Editors;

/// <summary>
/// Shared Foxit-style (Prototype A) nav icons for document-copies catalog sections.
/// </summary>
internal static class DocumentCopiesCatalogNavIcons
{
    public const string Documents = "Documents";
    public const string LinkedDocuments = "LinkedDocuments";
    public const string ApplicationForm = "ApplicationForm";

    public static string CssClass(string sectionId)
    {
        var key = string.IsNullOrWhiteSpace(sectionId) ? "default" : sectionId.Trim().ToLowerInvariant();
        return key switch
        {
            "passports" => "doc-copies-catalog__nav-icon--passports",
            "education" => "doc-copies-catalog__nav-icon--education",
            "addresses" => "doc-copies-catalog__nav-icon--addresses",
            "workpermits" => "doc-copies-catalog__nav-icon--workpermits",
            "invitations" => "doc-copies-catalog__nav-icon--invitations",
            "rejections" => "doc-copies-catalog__nav-icon--rejections",
            "medicalrecords" => "doc-copies-catalog__nav-icon--medicalrecords",
            "persondocuments" => "doc-copies-catalog__nav-icon--persondocuments",
            "familyrelationdocuments" => "doc-copies-catalog__nav-icon--familyrelationdocuments",
            "documents" => "doc-copies-catalog__nav-icon--documents",
            "linkeddocuments" => "doc-copies-catalog__nav-icon--linkeddocuments",
            "applicationform" => "doc-copies-catalog__nav-icon--applicationform",
            _ => "doc-copies-catalog__nav-icon--default",
        };
    }

    public static string Svg(string sectionId)
    {
        var key = string.IsNullOrWhiteSpace(sectionId) ? string.Empty : sectionId.Trim();
        return key switch
        {
            "Passports" => """<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><circle cx="12" cy="10" r="2.5"/><path d="M8 16.5h8"/></svg>""",
            "Education" => """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 9.5 12 5l9 4.5-9 4.5L3 9.5z"/><path d="M7 12.5v4.2c0 .6 2.2 1.8 5 1.8s5-1.2 5-1.8v-4.2"/><path d="M20 10.5v5"/></svg>""",
            "Addresses" => """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 21s-6.5-5.2-6.5-10.2A6.5 6.5 0 0 1 12 4.3a6.5 6.5 0 0 1 6.5 6.5C18.5 15.8 12 21 12 21z"/><circle cx="12" cy="10.5" r="2.2"/></svg>""",
            "WorkPermits" => """<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="7" width="18" height="13" rx="2"/><path d="M8 7V5.5A2.5 2.5 0 0 1 10.5 3h3A2.5 2.5 0 0 1 16 5.5V7"/><path d="M3 12h18"/></svg>""",
            "Invitations" => """<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="5" width="18" height="14" rx="2"/><path d="m3 7 9 6 9-6"/></svg>""",
            "Rejections" => """<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="8"/><path d="m9 9 6 6M15 9l-6 6"/></svg>""",
            "MedicalRecords" => """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M8 3h8v4H8z"/><rect x="5" y="7" width="14" height="14" rx="2"/><path d="M12 11v6M9 14h6"/></svg>""",
            "PersonDocuments" => """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 3h7l5 5v13a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z"/><path d="M14 3v5h5"/><path d="M9 13h6M9 17h4"/></svg>""",
            "FamilyRelationDocuments" => """<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="8" cy="8" r="2.5"/><circle cx="16" cy="8" r="2.5"/><path d="M4.5 18c.6-2.4 2.4-3.8 3.5-3.8S11 15.6 11.6 18"/><path d="M12.4 18c.6-2.4 2.4-3.8 3.5-3.8s2.9 1.4 3.5 3.8"/></svg>""",
            "Documents" => """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 3h7l5 5v13a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z"/><path d="M14 3v5h5"/><path d="M9 13h6M9 17h4"/></svg>""",
            "LinkedDocuments" => """<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M10 13a5 5 0 0 0 7.07 0l1.41-1.41a5 5 0 0 0-7.07-7.07L10 5.93"/><path d="M14 11a5 5 0 0 0-7.07 0L5.52 12.41a5 5 0 0 0 7.07 7.07L14 18.07"/></svg>""",
            "ApplicationForm" => """<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="M8 8h8M8 12h8M8 16h5"/></svg>""",
            _ => """<svg viewBox="0 0 24 24" aria-hidden="true"><rect x="5" y="4" width="14" height="16" rx="2"/><path d="M8 9h8M8 13h8M8 17h5"/></svg>""",
        };
    }
}