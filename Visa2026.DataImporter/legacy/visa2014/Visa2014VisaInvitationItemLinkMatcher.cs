using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Path B InvitationItem closest-match input (target-side; unit-testable without DB).
/// </summary>
internal sealed class Visa2014VisaInvitationItemLinkCandidate
{
    public Guid InvitationItemId { get; init; }
    public Guid InvitationId { get; init; }
    public Guid PersonId { get; init; }
    public Guid ApplicationId { get; init; }
    public DateTime IssuedDate { get; init; }
    public DateTime ApplicationDate { get; init; }
    public bool IsCancelled { get; init; }
    public bool IsChanged { get; init; }
    public bool IsUsed { get; init; }
}

/// <summary>
/// Path B: pick closest unused InvitationItem under the issuing application for a visa holder.
/// Does not call Module Path A helpers.
/// </summary>
internal static class Visa2014VisaInvitationItemLinkMatcher
{
    /// <summary>
    /// Returns the chosen InvitationItem id, or null when no eligible candidate.
    /// </summary>
    public static Guid? SelectClosest(
        Guid personId,
        Guid issuingApplicationId,
        DateTime visaIssueDate,
        IEnumerable<Visa2014VisaInvitationItemLinkCandidate> candidates,
        IReadOnlySet<Guid> invitationItemIdsLinkedToOtherVisas)
    {
        if (personId == Guid.Empty || issuingApplicationId == Guid.Empty || candidates == null)
            return null;

        var linked = invitationItemIdsLinkedToOtherVisas ?? new HashSet<Guid>();

        var eligible = candidates
            .Where(c => c.PersonId == personId)
            .Where(c => c.ApplicationId == issuingApplicationId)
            .Where(c => !c.IsCancelled && !c.IsChanged && !c.IsUsed)
            .Where(c => !linked.Contains(c.InvitationItemId))
            .ToList();

        if (eligible.Count == 0)
            return null;

        var preferred = eligible
            .Where(c => c.IssuedDate.Date > c.ApplicationDate.Date)
            .ToList();
        var pool = preferred.Count > 0 ? preferred : eligible;

        if (visaIssueDate != default)
        {
            var beforeVisa = pool
                .Where(c => c.IssuedDate.Date < visaIssueDate.Date)
                .Select(c => new
                {
                    c.InvitationItemId,
                    GapDays = (visaIssueDate.Date - c.IssuedDate.Date).TotalDays,
                    c.InvitationId
                })
                .OrderBy(x => x.GapDays)
                .ThenByDescending(x => x.InvitationId)
                .ToList();

            return beforeVisa.Count > 0 ? beforeVisa[0].InvitationItemId : null;
        }

        return pool
            .OrderByDescending(c => c.IssuedDate)
            .ThenByDescending(c => c.InvitationId)
            .Select(c => (Guid?)c.InvitationItemId)
            .FirstOrDefault();
    }
}