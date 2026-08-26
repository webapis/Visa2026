using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.HeaderLinkedDocuments;

public static class HeaderDocumentCopiesLocalization
{
    public static string TitleKey(HeaderDocumentCopiesFamily family) =>
        family switch
        {
            HeaderDocumentCopiesFamily.WorkPermit => "WorkPermitDocumentCopies.Title",
            HeaderDocumentCopiesFamily.Invitation => "InvitationDocumentCopies.Title",
            HeaderDocumentCopiesFamily.Rejection => "RejectionDocumentCopies.Title",
            HeaderDocumentCopiesFamily.BorderZone => "BorderZoneDocumentCopies.Title",
            HeaderDocumentCopiesFamily.Visa => "Visa.Tab.DocumentCopies",
            _ => "HeaderDocumentCopies.Title",
        };

    public static string Title(HeaderDocumentCopiesFamily family) =>
        VisaUiMessages.Get(TitleKey(family));

    public static string Title(HeaderDocumentCopiesFamily family, string culture) =>
        VisaUiMessages.Get(TitleKey(family), culture);

    public static string ListSelectOneKey(HeaderDocumentCopiesFamily family) =>
        family switch
        {
            HeaderDocumentCopiesFamily.WorkPermit => "WorkPermitDocumentCopies.List.SelectOne",
            HeaderDocumentCopiesFamily.Invitation => "InvitationDocumentCopies.List.SelectOne",
            HeaderDocumentCopiesFamily.Rejection => "RejectionDocumentCopies.List.SelectOne",
            HeaderDocumentCopiesFamily.BorderZone => "BorderZoneDocumentCopies.List.SelectOne",
            _ => "HeaderDocumentCopies.List.SelectOne",
        };

    public static string ListColumnLinkKey(HeaderDocumentCopiesFamily family) =>
        family switch
        {
            HeaderDocumentCopiesFamily.WorkPermit => "WorkPermitDocumentCopies.List.ColumnLink",
            HeaderDocumentCopiesFamily.Invitation => "InvitationDocumentCopies.List.ColumnLink",
            HeaderDocumentCopiesFamily.Rejection => "RejectionDocumentCopies.List.ColumnLink",
            HeaderDocumentCopiesFamily.BorderZone => "BorderZoneDocumentCopies.List.ColumnLink",
            _ => "HeaderDocumentCopies.List.ColumnLink",
        };
}
