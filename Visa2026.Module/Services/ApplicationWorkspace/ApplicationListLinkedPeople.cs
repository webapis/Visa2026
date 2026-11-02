using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// People stacked on an application list row, each with one mark per result
/// that application produces. Green means that person's document exists,
/// orange means it does not. A rejection mark appears only for a person who
/// has one, in the same red as the application result chip.
/// </summary>
public static class ApplicationListLinkedPeople
{
    public const string ToneDone = "done";
    public const string ToneMissing = "none";
    public const string ToneRejected = "rej";

    public readonly record struct Mark(string Key, string Tone);

    public readonly record struct Row(Guid PersonId, string Name, IReadOnlyList<Mark> Marks);

    public static IReadOnlyList<Row> Build(
        ApplicationProfileInstance? application,
        IReadOnlyCollection<Guid>? activeRoster,
        IReadOnlyDictionary<Guid, string> names,
        IReadOnlySet<Guid> invitation,
        IReadOnlySet<Guid> workPermit,
        IReadOnlySet<Guid> visa,
        IReadOnlySet<Guid> borderZone,
        IReadOnlySet<Guid> rejection)
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
                marks.Add(new Mark(kind, holders.Contains(personId) ? ToneDone : ToneMissing));
            }

            if (rejection.Contains(personId))
                marks.Add(new Mark(ApplicationWorkspaceIssuedRecordsCatalog.Rejection, ToneRejected));

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
        _ => key,
    };

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
        _ => key,
    };

    private static readonly IReadOnlySet<Guid> Empty = new HashSet<Guid>();
}
