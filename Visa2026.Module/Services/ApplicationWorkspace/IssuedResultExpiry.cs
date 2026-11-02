using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// An issued visa or work permit whose end date is before today.
/// A document that was never issued is not expired.
/// </summary>
public static class IssuedResultExpiry
{
    public const string WorkPermit = "workPermitExpired";
    public const string Visa = "visaExpired";
    public const string Tone = InvitationLaterLife.ToneExpired;

    public static bool IsExpiryKey(string? key) =>
        string.Equals(key, WorkPermit, StringComparison.Ordinal)
        || string.Equals(key, Visa, StringComparison.Ordinal);

    /// <summary>
    /// People whose every dated document is already past. A missing date is not an expiry,
    /// and one document that is still in force keeps the person out of this set.
    /// </summary>
    public static Dictionary<Guid, HashSet<Guid>> People(
        IEnumerable<(Guid InstanceId, Guid PersonId, DateTime? Expiration)> rows)
    {
        var result = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var group in rows.GroupBy(row => (row.InstanceId, row.PersonId)))
        {
            var dated = false;
            var allExpired = true;
            foreach (var row in group)
            {
                if (row.Expiration is not DateTime expiration || expiration == default)
                    continue;

                dated = true;
                if (!ExpirationLogicHelper.IsExpired(expiration))
                    allExpired = false;
            }

            if (!dated || !allExpired)
                continue;

            if (!result.TryGetValue(group.Key.InstanceId, out var people))
            {
                people = new HashSet<Guid>();
                result[group.Key.InstanceId] = people;
            }

            people.Add(group.Key.PersonId);
        }

        return result;
    }
}
