using System;
using System.Linq;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Users-role grants for Person soft incomplete flag (Mark incomplete / Mark complete actions).
/// </summary>
public static class PersonIncompleteDataOfficerPermissions
{
    private static readonly string[] PersonIncompleteMemberNames =
    [
        nameof(Person.IsDataIncomplete),
        nameof(Person.IncompleteMissingPersonalData),
        nameof(Person.IncompleteMissingPassport),
        nameof(Person.IncompleteMissingCv),
        nameof(Person.IncompleteMissingPhoto),
        nameof(Person.IncompleteMissingEducation),
        nameof(Person.IncompleteMissingMedical),
        nameof(Person.IncompleteMissingAddress),
        nameof(Person.IncompleteMissingFamilyDocs),
        nameof(Person.IncompleteMissingOther),
        nameof(Person.IncompleteNotes),
        nameof(Person.IncompleteMarkedOn),
        nameof(Person.IncompleteMarkedBy),
    ];

    public static void Ensure(PermissionPolicyRole role)
    {
        if (role == null)
        {
            return;
        }

        EnsurePopupDialogPermissions(role);

        foreach (string memberName in PersonIncompleteMemberNames)
        {
            EnsureMemberWritePermission<Person>(role, memberName);
        }
    }

    private static void EnsurePopupDialogPermissions(PermissionPolicyRole role)
    {
        var targetType = typeof(PersonIncompleteMarkOptions);
        var existingPerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
        if (existingPerm != null)
        {
            existingPerm.ReadState = SecurityPermissionState.Allow;
            existingPerm.WriteState = SecurityPermissionState.Allow;
            existingPerm.CreateState = SecurityPermissionState.Allow;
            existingPerm.DeleteState = null;
            existingPerm.NavigateState = SecurityPermissionState.Allow;
            return;
        }

        const string readWriteCreate = $"{SecurityOperations.Read};{SecurityOperations.Write};{SecurityOperations.Create}";
        role.AddTypePermission<PersonIncompleteMarkOptions>(readWriteCreate, SecurityPermissionState.Allow);
        var newPerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
        if (newPerm != null)
        {
            newPerm.NavigateState = SecurityPermissionState.Allow;
        }
    }

    private static void EnsureMemberWritePermission<T>(PermissionPolicyRole role, string memberName) where T : class
    {
        var typePerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == typeof(T));
        if (typePerm == null)
        {
            role.AddTypePermissionsRecursively<T>(SecurityOperations.Read, SecurityPermissionState.Allow);
            typePerm = role.TypePermissions.First(p => p.TargetType == typeof(T));
        }

        var memberPerm = typePerm.MemberPermissions
            .FirstOrDefault(mp => string.Equals(mp.Members, memberName, StringComparison.Ordinal));
        if (memberPerm != null)
        {
            memberPerm.WriteState = SecurityPermissionState.Allow;
            return;
        }

        role.AddMemberPermissionFromLambda<T>(
            SecurityOperations.Write,
            memberName,
            _ => true,
            SecurityPermissionState.Allow);
    }
}
