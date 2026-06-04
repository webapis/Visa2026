using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module;

/// <summary>Criteria strings and role assignment helpers for <see cref="Person"/>.</summary>
public static class PersonRoleHelper
{
    public const string EmployeeCriteria =
        "PersonRole = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,Employee#";

    public const string FamilyMemberCriteria =
        "PersonRole = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,FamilyMember#";

    public const string TemporaryVisitorCriteria =
        "PersonRole = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,TemporaryVisitor#";

    public const string NotEmployeeCriteria =
        "PersonRole != ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,Employee#";

    public const string IsEmployeeRoleCriteria = EmployeeCriteria;

    public static void ApplyRole(Person person, PersonRecordRole role)
    {
        person.PersonRole = role;
        SyncIsEmployee(person);
        if (role == PersonRecordRole.TemporaryVisitor)
            ClearFamilyMemberLinks(person);
    }

    public static void SyncIsEmployee(Person person) =>
        person.IsEmployee = person.PersonRole == PersonRecordRole.Employee;

    public static void ClearFamilyMemberLinks(Person person)
    {
        person.SponsoringEmployee = null;
        person.Relationship = null;
    }
}
