using System;
using System.Linq;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Visa2026.Module.BusinessObjects.Email;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Users-role grants for emailing document copies: shared + own address book, office templates,
/// read-only sent history. <see cref="OfficerMailConnection"/> gets no grant (host services only).
/// </summary>
public static class EmailDocumentCopiesOfficerPermissions
{
    private const string EmailNavigationRoot = @"Application/NavigationItems/Items/Email";

    public static void Ensure(PermissionPolicyRole role)
    {
        if (role == null)
            return;

        EnsureContactPermissions(role);
        EnsureFullAccess<EmailTemplate>(role);
        EnsureReadCreate<SentEmail>(role);
        EnsureReadCreate<SentEmailPerson>(role);
        DenyAll<OfficerMailConnection>(role);
        DenyAll<EmailSmtpSettings>(role);

        EnsureNavigation(role, EmailNavigationRoot);
        EnsureNavigation(role, $"{EmailNavigationRoot}/Items/{nameof(EmailContact)}");
        EnsureNavigation(role, $"{EmailNavigationRoot}/Items/{nameof(EmailTemplate)}");
        EnsureNavigation(role, $"{EmailNavigationRoot}/Items/{nameof(SentEmail)}");
    }

    private static void EnsureContactPermissions(PermissionPolicyRole role)
    {
        var targetType = typeof(EmailContact);
        var typePerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
        if (typePerm == null)
        {
            role.AddTypePermissionsRecursively<EmailContact>(SecurityOperations.Create, SecurityPermissionState.Allow);
            typePerm = role.TypePermissions.First(p => p.TargetType == targetType);
        }

        typePerm.CreateState = SecurityPermissionState.Allow;
        typePerm.ReadState = null;
        typePerm.WriteState = null;
        typePerm.DeleteState = null;
        typePerm.NavigateState = SecurityPermissionState.Allow;

        if (!typePerm.ObjectPermissions.Any(op => op.ReadState == SecurityPermissionState.Allow))
        {
            role.AddObjectPermissionFromLambda<EmailContact>(
                $"{SecurityOperations.Read};{SecurityOperations.Write};{SecurityOperations.Delete}",
                c => c.IsShared || c.Owner.ID == (Guid)CurrentUserIdOperator.CurrentUserId(),
                SecurityPermissionState.Allow);
        }
    }

    private static void EnsureFullAccess<T>(PermissionPolicyRole role) where T : class
    {
        var perm = role.TypePermissions.FirstOrDefault(p => p.TargetType == typeof(T));
        if (perm == null)
        {
            role.AddTypePermissionsRecursively<T>(SecurityOperations.FullAccess, SecurityPermissionState.Allow);
            return;
        }

        perm.ReadState = SecurityPermissionState.Allow;
        perm.WriteState = SecurityPermissionState.Allow;
        perm.CreateState = SecurityPermissionState.Allow;
        perm.DeleteState = SecurityPermissionState.Allow;
        perm.NavigateState = SecurityPermissionState.Allow;
    }

    private static void EnsureReadCreate<T>(PermissionPolicyRole role) where T : class
    {
        var perm = role.TypePermissions.FirstOrDefault(p => p.TargetType == typeof(T));
        if (perm == null)
        {
            role.AddTypePermissionsRecursively<T>(
                $"{SecurityOperations.Read};{SecurityOperations.Create}",
                SecurityPermissionState.Allow);
            perm = role.TypePermissions.First(p => p.TargetType == typeof(T));
        }

        perm.ReadState = SecurityPermissionState.Allow;
        perm.CreateState = SecurityPermissionState.Allow;
        perm.WriteState = SecurityPermissionState.Deny;
        perm.DeleteState = SecurityPermissionState.Deny;
        perm.NavigateState = SecurityPermissionState.Allow;
    }

    private static void DenyAll<T>(PermissionPolicyRole role) where T : class
    {
        var perm = role.TypePermissions.FirstOrDefault(p => p.TargetType == typeof(T));
        if (perm == null)
        {
            role.AddTypePermissionsRecursively<T>(SecurityOperations.FullAccess, SecurityPermissionState.Deny);
            return;
        }

        perm.ReadState = SecurityPermissionState.Deny;
        perm.WriteState = SecurityPermissionState.Deny;
        perm.CreateState = SecurityPermissionState.Deny;
        perm.DeleteState = SecurityPermissionState.Deny;
        perm.NavigateState = SecurityPermissionState.Deny;
    }

    private static void EnsureNavigation(PermissionPolicyRole role, string itemPath)
    {
        var existing = role.NavigationPermissions.FirstOrDefault(p => p.ItemPath == itemPath);
        if (existing == null)
            role.AddNavigationPermission(itemPath, SecurityPermissionState.Allow);
        else
            existing.NavigateState = SecurityPermissionState.Allow;
    }
}
