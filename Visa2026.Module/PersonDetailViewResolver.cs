using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module;

/// <summary>Chooses the typed Person detail view for list navigation and object state.</summary>
public static class PersonDetailViewResolver
{
    public static string Resolve(string? listViewId, Person person)
    {
        if (listViewId == "Person_ListView_Employees")
            return PersonDetailViewIds.Employee;

        if (listViewId == "Person_ListView_FamilyMembers")
            return PersonDetailViewIds.FamilyMember;

        if (listViewId == "Person_ListView_TemporaryVisitors")
            return PersonDetailViewIds.TemporaryVisitor;

        return person.PersonRole switch
        {
            PersonRecordRole.Employee => PersonDetailViewIds.Employee,
            PersonRecordRole.TemporaryVisitor => PersonDetailViewIds.TemporaryVisitor,
            _ => PersonDetailViewIds.FamilyMember,
        };
    }
}
