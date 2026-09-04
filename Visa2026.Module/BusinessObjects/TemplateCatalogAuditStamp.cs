#nullable enable

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Quiet created/updated stamps for Resminamalar catalog rows (nested profile templates and user templates).
/// </summary>
public static class TemplateCatalogAuditStamp
{
    public static void Touch(ApplicationProfileTemplate template, string? userName)
    {
        ArgumentNullException.ThrowIfNull(template);
        var now = DateTime.UtcNow;
        var who = NormalizeUser(userName);
        if (template.CreatedOnUtc == null)
        {
            template.CreatedOnUtc = now;
            template.CreatedByUserName = who;
        }

        template.ModifiedOnUtc = now;
        template.ModifiedByUserName = who ?? template.CreatedByUserName;
    }

    public static void Touch(UserReportTemplate template, string? userName)
    {
        ArgumentNullException.ThrowIfNull(template);
        var now = DateTime.UtcNow;
        var who = NormalizeUser(userName);
        if (template.CreatedOnUtc == null)
        {
            template.CreatedOnUtc = now;
            template.CreatedByUserName = who;
        }

        template.ModifiedOnUtc = now;
        template.ModifiedByUserName = who ?? template.CreatedByUserName;
    }

    public static string? FormatQuietLine(
        DateTime? createdOnUtc,
        string? createdByUserName,
        DateTime? modifiedOnUtc,
        string? modifiedByUserName)
    {
        var useModified = modifiedOnUtc != null
            && (createdOnUtc == null || modifiedOnUtc.Value > createdOnUtc.Value.AddSeconds(2));
        var whenUtc = useModified ? modifiedOnUtc : createdOnUtc;
        if (whenUtc == null)
            return null;

        var who = useModified ? modifiedByUserName : createdByUserName;
        if (string.IsNullOrWhiteSpace(who) && !useModified)
            who = modifiedByUserName;
        var stamp = ToLocalStamp(whenUtc.Value);
        return string.IsNullOrWhiteSpace(who) ? stamp : stamp + " · " + who.Trim();
    }

    public static string? FormatQuietTitle(
        DateTime? createdOnUtc,
        string? createdByUserName,
        DateTime? modifiedOnUtc,
        string? modifiedByUserName)
    {
        var created = createdOnUtc == null
            ? null
            : JoinStamp(ToLocalStamp(createdOnUtc.Value), createdByUserName);
        var updated = modifiedOnUtc == null
            ? null
            : JoinStamp(ToLocalStamp(modifiedOnUtc.Value), modifiedByUserName);
        if (created == null)
            return updated == null ? null : "Updated " + updated;
        if (updated == null || updated == created)
            return "Created " + created;
        return "Created " + created + " · Updated " + updated;
    }

    private static string? NormalizeUser(string? userName)
    {
        var trimmed = userName?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 255)
            return string.IsNullOrEmpty(trimmed) ? null : trimmed[..255];
        return trimmed;
    }

    private static string ToLocalStamp(DateTime utc)
    {
        var value = utc.Kind == DateTimeKind.Utc
            ? utc.ToLocalTime()
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
        return value.ToString("g");
    }

    private static string JoinStamp(string stamp, string? userName) =>
        string.IsNullOrWhiteSpace(userName) ? stamp : stamp + " · " + userName.Trim();
}