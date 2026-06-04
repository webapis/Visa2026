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

        return person.IsEmployee
            ? PersonDetailViewIds.Employee
            : PersonDetailViewIds.FamilyMember;
    }
}
