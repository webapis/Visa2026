using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonLink;

public sealed class ApplicationProfileInstancePersonLinkQueryService : IApplicationProfileInstancePersonLinkQueryService
{
    public IReadOnlyList<ApplicationProfileInstancePersonLinkCandidateRow> SearchCandidates(
        IObjectSpace objectSpace,
        Guid applicationId,
        string? searchText,
        int maxResults = 25)
    {
        if (objectSpace == null || applicationId == Guid.Empty || maxResults <= 0)
            return Array.Empty<ApplicationProfileInstancePersonLinkCandidateRow>();

        var notLinked = CriteriaOperator.Parse(
            "Not [ApplicationProfileInstances][ID = ?]",
            applicationId);

        var identity = PersonListViewFullTextSearchCriteriaBuilder.BuildPersonIdentityCriteria(searchText ?? string.Empty);
        var passport = PersonListViewFullTextSearchCriteriaBuilder.BuildPassportNumberCriteria(searchText ?? string.Empty);
        var search = PersonListViewFullTextSearchCriteriaBuilder.CombineOr(identity, passport);

        var criteria = search == null
            ? notLinked
            : GroupOperator.And(notLinked, search);

        return objectSpace.GetObjects<Person>(criteria, true)
            .Cast<Person>()
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(maxResults)
            .Select(MapRow)
            .ToList();
    }

    private static ApplicationProfileInstancePersonLinkCandidateRow MapRow(Person person)
    {
        return new ApplicationProfileInstancePersonLinkCandidateRow
        {
            PersonId = person.ID,
            FullName = person.FullName,
            PersonalNumber = string.IsNullOrWhiteSpace(person.PersonalNumber) ? "—" : person.PersonalNumber.Trim(),
            RoleLabel = person.PersonRole.ToString(),
            PassportNumber = ResolvePassportNumber(person),
            HasPhoto = person.Photo is { Length: > 0 },
        };
    }

    private static string ResolvePassportNumber(Person person)
    {
        var passports = person.Passports;
        if (passports == null || passports.Count == 0)
            return "—";

        return passports
            .OrderByDescending(p => p.IssueDate ?? DateTime.MinValue)
            .Select(p => p.PassportNumber)
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
            ?? "—";
    }
}
