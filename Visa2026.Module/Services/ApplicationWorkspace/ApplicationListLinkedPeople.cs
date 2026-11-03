using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// People stacked on an application list row, each with one mark per result
/// that application produces. Green means that person's document exists.
/// Blue means a visa, invitation, or work permit is not issued yet. Pink
/// means an issued one is expired. An issued invitation keeps one circle:
/// the letters stay Çak and the fill is that person's later life. A rejection
/// mark appears only for a person who has one, in the same red as the
/// application result chip.
/// </summary>
public static class ApplicationListLinkedPeople
{
    public const string ToneDone = "done";
    public const string ToneMissing = "none";
    public const string ToneRejected = "rej";

    /// <param name="State">Later life of an issued invitation, or expiry of an issued visa or work permit.</param>
    /// <param name="DocumentId">Header of the issued copy. Empty when the circle is not a link.</param>
    public readonly record struct Mark(string Key, string Tone, string? State = null, Guid DocumentId = default);

    public readonly record struct Row(Guid PersonId, string Name, IReadOnlyList<Mark> Marks);

    public static IReadOnlyList<Row> Build(
        ApplicationProfileInstance? application,
        IReadOnlyCollection<Guid>? activeRoster,
        IReadOnlyDictionary<Guid, string> names,
        IReadOnlySet<Guid> invitation,
        IReadOnlySet<Guid> workPermit,
        IReadOnlySet<Guid> visa,
        IReadOnlySet<Guid> borderZone,
        IReadOnlySet<Guid> rejection,
        IReadOnlyDictionary<Guid, string>? invitationLaterLife = null,
        IReadOnlySet<Guid>? expiredWorkPermit = null,
        IReadOnlySet<Guid>? expiredVisa = null,
        IReadOnlyDictionary<Guid, Guid>? invitationDocuments = null,
        IReadOnlyDictionary<Guid, Guid>? workPermitDocuments = null,
        IReadOnlyDictionary<Guid, Guid>? visaDocuments = null,
        IReadOnlyDictionary<Guid, Guid>? borderZoneDocuments = null,
        IReadOnlyDictionary<Guid, Guid>? rejectionDocuments = null)
    {
        if (application == null || activeRoster == null || activeRoster.Count == 0)
            return Array.Empty<Row>();

        var kinds = VisibleKinds(application);
        var rows = new List<Row>(activeRoster.Count);
        foreach (var personId in activeRoster.OrderBy(id => NameOf(names, id), StringComparer.CurrentCultureIgnoreCase))
        {
            var marks = new List<Mark>(kinds.Count + 1);
            foreach (var kind in kinds)
            {
                var holders = Holders(kind, invitation, workPermit, visa, borderZone);
                var issued = holders.Contains(personId);
                var tone = issued ? ToneDone : ToneMissing;
                string? state = null;
                if (issued
                    && string.Equals(kind, ApplicationWorkspaceIssuedRecordsCatalog.Invitation, StringComparison.Ordinal)
                    && invitationLaterLife != null
                    && invitationLaterLife.TryGetValue(personId, out var laterLife)
                    && !string.IsNullOrEmpty(laterLife))
                {
                    state = laterLife;
                    tone = InvitationLaterLife.Tone(laterLife);
                }
                else if (issued && ExpiredState(kind, personId, expiredWorkPermit, expiredVisa) is string expired)
                {
                    state = expired;
                    tone = IssuedResultExpiry.Tone;
                }

                marks.Add(new Mark(
                    kind,
                    tone,
                    state,
                    issued ? DocumentOf(kind, personId, invitationDocuments, workPermitDocuments, visaDocuments, borderZoneDocuments) : Guid.Empty));
            }

            if (rejection.Contains(personId))
            {
                marks.Add(new Mark(
                    ApplicationWorkspaceIssuedRecordsCatalog.Rejection,
                    ToneRejected,
                    DocumentId: DocumentOf(
                        ApplicationWorkspaceIssuedRecordsCatalog.Rejection,
                        personId,
                        rejectionDocuments: rejectionDocuments)));
            }

            rows.Add(new Row(personId, NameOf(names, personId), marks));
        }

        return rows;
    }

    public static string Letters(string key) =>
        VisaUiMessages.Get(MarkKey(key));

    public static string Title(string key) => key switch
    {
        ApplicationWorkspaceIssuedRecordsCatalog.Invitation =>
            VisaUiMessages.Get("ApplicationProfile.Doc.Invitation"),
        ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit =>
            VisaUiMessages.Get("ApplicationProfile.Doc.WorkPermit"),
        ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa =>
            VisaUiMessages.Get("ApplicationProfile.Doc.Visa"),
        ApplicationWorkspaceIssuedRecordsCatalog.BorderZone =>
            VisaUiMessages.Get("ApplicationProfile.Doc.BorderZone"),
        ApplicationWorkspaceIssuedRecordsCatalog.Rejection =>
            VisaUiMessages.Get("ApplicationProfile.Doc.Rejection"),
        IssuedResultExpiry.WorkPermit or IssuedResultExpiry.Visa =>
            VisaUiMessages.Get("ApplicationProfileInstance.List.Mark.DocumentExpiredTitle"),
        InvitationLaterLife.Used =>
            VisaUiMessages.Get("ApplicationProfileInstance.List.Mark.InvitationUsedTitle"),
        InvitationLaterLife.ExpiredUnused =>
            VisaUiMessages.Get("ApplicationProfileInstance.List.Mark.InvitationExpiredTitle"),
        InvitationLaterLife.Cancelled =>
            VisaUiMessages.Get("ApplicationProfileInstance.List.Mark.InvitationCancelledTitle"),
        InvitationLaterLife.Changed =>
            VisaUiMessages.Get("ApplicationProfileInstance.List.Mark.InvitationChangedTitle"),
        _ => key,
    };

    /// <summary>
    /// An issued circle with a real header opens that copy. A circle that was not issued stays plain text.
    /// </summary>
    public static bool CanPreview(Mark mark) =>
        mark.Tone != ToneMissing
        && mark.DocumentId != Guid.Empty
        && ApplicationWorkspaceIssuedRecordsCatalog.TryGetDocumentCopiesFamily(mark.Key, out _);

    public static string Title(Mark mark)
    {
        if (!string.IsNullOrEmpty(mark.State))
            return Title(mark.State);
        if (mark.Tone == ToneMissing && IsVisaInvitationOrWorkPermit(mark.Key))
            return VisaUiMessages.Get("ApplicationProfileInstance.List.Mark.NotIssuedTitle");
        return Title(mark.Key);
    }

    public static int CountMarks(IReadOnlyList<Row>? rows, string key)
    {
        if (rows == null || rows.Count == 0 || string.IsNullOrEmpty(key))
            return 0;

        var count = 0;
        foreach (var row in rows)
        {
            foreach (var mark in row.Marks)
            {
                if (string.Equals(mark.Key, key, StringComparison.Ordinal)
                    || string.Equals(mark.State, key, StringComparison.Ordinal))
                    count++;
            }
        }

        return count;
    }

    public static IReadOnlyDictionary<Guid, string> LoadNames(
        IObjectSpace? objectSpace,
        IReadOnlyDictionary<Guid, HashSet<Guid>> roster)
    {
        var ids = new HashSet<Guid>();
        foreach (var people in roster.Values)
            ids.UnionWith(people);

        if (objectSpace == null || ids.Count == 0)
            return new Dictionary<Guid, string>();

        return objectSpace.GetObjectsQuery<Person>()
            .Where(p => ids.Contains(p.ID))
            .Select(p => new { p.ID, p.FirstName, p.MiddleName, p.LastName })
            .AsEnumerable()
            .ToDictionary(p => p.ID, p => FormatName(p.FirstName, p.MiddleName, p.LastName));
    }

    public static string FormatName(string? first, string? middle, string? last)
    {
        var name = string.Join(
            ' ',
            new[] { first, middle, last }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return string.IsNullOrWhiteSpace(name) ? "—" : name.Trim();
    }

    private static List<string> VisibleKinds(ApplicationProfileInstance application)
    {
        var kinds = new List<string>();
        foreach (var def in ApplicationWorkspaceIssuedRecordsCatalog.Definitions)
        {
            if (def.IsOptional)
                continue;
            if (!def.IsVisible(application))
                continue;
            kinds.Add(def.Key);
        }

        return kinds;
    }

    private static IReadOnlySet<Guid> Holders(
        string key,
        IReadOnlySet<Guid> invitation,
        IReadOnlySet<Guid> workPermit,
        IReadOnlySet<Guid> visa,
        IReadOnlySet<Guid> borderZone) => key switch
    {
        ApplicationWorkspaceIssuedRecordsCatalog.Invitation => invitation,
        ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit => workPermit,
        ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa => visa,
        ApplicationWorkspaceIssuedRecordsCatalog.BorderZone => borderZone,
        _ => Empty,
    };

    private static string NameOf(IReadOnlyDictionary<Guid, string> names, Guid personId) =>
        names.TryGetValue(personId, out var name) && !string.IsNullOrWhiteSpace(name) ? name : "—";

    private static string MarkKey(string key) => key switch
    {
        ApplicationWorkspaceIssuedRecordsCatalog.Invitation =>
            "ApplicationProfileInstance.List.Mark.Invitation",
        ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit =>
            "ApplicationProfileInstance.List.Mark.WorkPermit",
        ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa =>
            "ApplicationProfileInstance.List.Mark.Visa",
        ApplicationWorkspaceIssuedRecordsCatalog.BorderZone =>
            "ApplicationProfileInstance.List.Mark.BorderZone",
        ApplicationWorkspaceIssuedRecordsCatalog.Rejection =>
            "ApplicationProfileInstance.List.Mark.Rejection",
        IssuedResultExpiry.WorkPermit or IssuedResultExpiry.Visa =>
            "ApplicationProfileInstance.List.Mark.InvitationExpired",
        InvitationLaterLife.Used =>
            "ApplicationProfileInstance.List.Mark.InvitationUsed",
        InvitationLaterLife.ExpiredUnused =>
            "ApplicationProfileInstance.List.Mark.InvitationExpired",
        InvitationLaterLife.Cancelled =>
            "ApplicationProfileInstance.List.Mark.InvitationCancelled",
        InvitationLaterLife.Changed =>
            "ApplicationProfileInstance.List.Mark.InvitationChanged",
        _ => key,
    };

    private static string? ExpiredState(
        string kind,
        Guid personId,
        IReadOnlySet<Guid>? expiredWorkPermit,
        IReadOnlySet<Guid>? expiredVisa)
    {
        if (string.Equals(kind, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, StringComparison.Ordinal)
            && expiredWorkPermit != null
            && expiredWorkPermit.Contains(personId))
            return IssuedResultExpiry.WorkPermit;

        if (string.Equals(kind, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.Ordinal)
            && expiredVisa != null
            && expiredVisa.Contains(personId))
            return IssuedResultExpiry.Visa;

        return null;
    }

    private static Guid DocumentOf(
        string key,
        Guid personId,
        IReadOnlyDictionary<Guid, Guid>? invitationDocuments = null,
        IReadOnlyDictionary<Guid, Guid>? workPermitDocuments = null,
        IReadOnlyDictionary<Guid, Guid>? visaDocuments = null,
        IReadOnlyDictionary<Guid, Guid>? borderZoneDocuments = null,
        IReadOnlyDictionary<Guid, Guid>? rejectionDocuments = null)
    {
        var documents = key switch
        {
            ApplicationWorkspaceIssuedRecordsCatalog.Invitation => invitationDocuments,
            ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit => workPermitDocuments,
            ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa => visaDocuments,
            ApplicationWorkspaceIssuedRecordsCatalog.BorderZone => borderZoneDocuments,
            ApplicationWorkspaceIssuedRecordsCatalog.Rejection => rejectionDocuments,
            _ => null,
        };
        return documents != null && documents.TryGetValue(personId, out var documentId) ? documentId : Guid.Empty;
    }

    private static bool IsVisaInvitationOrWorkPermit(string key) =>
        string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.Invitation, StringComparison.Ordinal)
        || string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, StringComparison.Ordinal)
        || string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.Ordinal);

    private static readonly IReadOnlySet<Guid> Empty = new HashSet<Guid>();
}
