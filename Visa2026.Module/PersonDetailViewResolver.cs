using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module;

/// <summary>Chooses the typed Person detail view for list navigation and object state.</summary>
public static class PersonDetailViewResolver
{
    public static string Resolve(string? listViewId, Person person)
    {
        if (PersonRosterListViewIds.IsEmployeeRoster(listViewId))
            return PersonDetailViewIds.Employee;

        if (PersonRosterListViewIds.IsFamilyRoster(listViewId))
            return PersonDetailViewIds.FamilyMember;

        if (listViewId == PersonRosterListViewIds.TemporaryVisitors)
            return PersonDetailViewIds.TemporaryVisitor;

        return person.PersonRole switch
        {
            PersonRecordRole.Employee => PersonDetailViewIds.Employee,
            PersonRecordRole.TemporaryVisitor => PersonDetailViewIds.TemporaryVisitor,
            _ => PersonDetailViewIds.FamilyMember,
        };
    }
}
