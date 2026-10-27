using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.Email;

namespace Visa2026.Module.Services.EmailDocumentCopies;

public sealed class EmailContactSuggestion
{
    public string Email { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? Organization { get; init; }

    public bool IsShared { get; init; }

    public int TimesUsed { get; init; }
}

/// <summary>Address book lookups and usage tracking scoped to one officer (shared + own contacts).</summary>
public static class EmailContactDirectory
{
    private static readonly Regex EmailRegex = new(EmailContact.EmailPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && email.Length <= 320 && EmailRegex.IsMatch(email.Trim());

    public static IQueryable<EmailContact> Visible(IObjectSpace objectSpace, Guid userId) =>
        objectSpace.GetObjectsQuery<EmailContact>()
            .Where(c => c.IsShared || (c.Owner != null && c.Owner.ID == userId));

    public static IReadOnlyList<EmailContactSuggestion> Search(IObjectSpace objectSpace, Guid userId, string? text, int take = 8)
    {
        var query = Visible(objectSpace, userId);
        var term = text?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var lowered = term.ToLower();
            query = query.Where(c =>
                (c.Email != null && c.Email.ToLower().Contains(lowered))
                || (c.Name != null && c.Name.ToLower().Contains(lowered))
                || (c.Organization != null && c.Organization.ToLower().Contains(lowered)));
        }

        return query
            .OrderByDescending(c => c.TimesUsed)
            .ThenBy(c => c.Name)
            .Take(take)
            .AsEnumerable()
            .Select(c => new EmailContactSuggestion
            {
                Email = c.Email,
                Name = c.Name,
                Organization = c.Organization,
                IsShared = c.IsShared,
                TimesUsed = c.TimesUsed,
            })
            .ToList();
    }

    /// <summary>Smallest remembered "max email size" among the given addresses, or null when none is set.</summary>
    public static int? RememberedMaxEmailSizeMb(IObjectSpace objectSpace, Guid userId, IEnumerable<string> emails)
    {
        var lowered = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
        if (lowered.Count == 0)
            return null;

        return Visible(objectSpace, userId)
            .Where(c => c.Email != null && lowered.Contains(c.Email.ToLower()) && c.MaxEmailSizeMb != null)
            .Select(c => c.MaxEmailSizeMb)
            .AsEnumerable()
            .Min();
    }

    /// <summary>
    /// Bumps usage on known addresses; optionally saves unknown ones as personal contacts. Remembers
    /// <paramref name="maxEmailSizeMb"/> on every recipient contact.
    /// </summary>
    public static void RecordUsage(
        IObjectSpace objectSpace,
        Guid userId,
        IEnumerable<EmailRecipient> recipients,
        bool saveNewAsPersonal,
        int? maxEmailSizeMb = null)
    {
        var now = DateTime.UtcNow;
        var owner = objectSpace.GetObjectByKey<ApplicationUser>(userId);
        foreach (var recipient in recipients.GroupBy(r => r.Email.Trim().ToLowerInvariant()).Select(g => g.First()))
        {
            var email = recipient.Email.Trim();
            var lowered = email.ToLowerInvariant();
            var existing = Visible(objectSpace, userId)
                .Where(c => c.Email != null && c.Email.ToLower() == lowered)
                .OrderByDescending(c => c.IsShared)
                .FirstOrDefault();

            if (existing != null)
            {
                existing.TimesUsed++;
                existing.LastUsedOnUtc = now;
                if (maxEmailSizeMb != null)
                    existing.MaxEmailSizeMb = maxEmailSizeMb;
                continue;
            }

            if (!saveNewAsPersonal)
                continue;

            var contact = objectSpace.CreateObject<EmailContact>();
            contact.Email = email;
            contact.Name = string.IsNullOrWhiteSpace(recipient.Name) ? null : recipient.Name.Trim();
            contact.IsShared = false;
            contact.Owner = owner;
            contact.TimesUsed = 1;
            contact.LastUsedOnUtc = now;
            contact.MaxEmailSizeMb = maxEmailSizeMb;
        }
    }
}
