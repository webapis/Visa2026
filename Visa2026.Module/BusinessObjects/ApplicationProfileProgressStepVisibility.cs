using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Which progress-timeline steps a profile shows. Every profile lists the same steps;
/// <see cref="Step.Visible"/> is the only difference. An empty
/// <see cref="ApplicationProfile.ProgressStepVisibility"/> keeps today's route rules.
/// </summary>
public static class ApplicationProfileProgressStepVisibility
{
    public const string Office = "office";
    public const string Ministry = "ministry";
    public const string Submission = "migration-submitted";
    public const string AsNumber = "migration";
    public const string Issued = "process-issued";
    public const string Rejected = "process-rejected";
    public const string Cancelled = "process-cancelled";

    public static readonly string[] AllKeys = [Office, Ministry, Submission, AsNumber, Issued, Rejected, Cancelled];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public sealed class Step
    {
        public string Key { get; set; } = string.Empty;

        public bool Visible { get; set; }

        public Step()
        {
        }

        public Step(string key, bool visible)
        {
            Key = key;
            Visible = visible;
        }
    }

    public static bool IsVisible(ApplicationProfile? profile, string key)
    {
        if (TryConfigured(profile?.ProgressStepVisibility, key, out var visible))
            return visible;

        return DefaultVisible(profile, key);
    }

    /// <summary>Today's timeline: via ministries include the AS-number step and Issued; direct Issuance/Change does too; other direct routes end at hand-in.</summary>
    public static bool DefaultVisible(ApplicationProfile? profile, string key)
    {
        if (string.Equals(key, Office, StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, Submission, StringComparison.OrdinalIgnoreCase))
            return true;

        var via = profile == null
            || profile.ProgressRoute == ApplicationProfileInstanceProgressRouteKind.ViaMinistries;
        if (string.Equals(key, Ministry, StringComparison.OrdinalIgnoreCase))
            return via;

        var showAsNumber = profile == null
            || via
            || (ApplicationProfileConfigurationResolver.DirectFinalStepRequiresAsNumber(profile)
                && !ApplicationProfileConfigurationResolver.DirectEndsAtMigrationSubmission(profile));
        if (string.Equals(key, AsNumber, StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, Issued, StringComparison.OrdinalIgnoreCase))
            return showAsNumber;

        // Last-step Rejected and Cancelled stay on direct Issuance/Change only.
        // Via ministry does not offer them on AS number received.
        if (string.Equals(key, Rejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, Cancelled, StringComparison.OrdinalIgnoreCase))
            return showAsNumber && profile != null && !via;

        return true;
    }

    public static string Normalize(ApplicationProfile profile, IReadOnlyList<Step>? configured)
    {
        var overlay = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        if (configured != null)
        {
            foreach (var step in configured)
            {
                if (!string.IsNullOrWhiteSpace(step.Key) && AllKeys.Contains(step.Key, StringComparer.OrdinalIgnoreCase))
                    overlay[step.Key] = step.Visible;
            }
        }

        var steps = new List<Step>(AllKeys.Length);
        foreach (var key in AllKeys)
        {
            steps.Add(new Step(
                key,
                overlay.TryGetValue(key, out var visible) ? visible : DefaultVisible(profile, key)));
        }

        return JsonSerializer.Serialize(steps, JsonOptions);
    }

    public static IReadOnlyList<Step> Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<Step>();

        try
        {
            return JsonSerializer.Deserialize<List<Step>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return Array.Empty<Step>();
        }
    }

    private static bool TryConfigured(string? json, string key, out bool visible)
    {
        visible = false;
        foreach (var step in Read(json))
        {
            if (!string.Equals(step.Key, key, StringComparison.OrdinalIgnoreCase))
                continue;

            visible = step.Visible;
            return true;
        }

        return false;
    }
}
