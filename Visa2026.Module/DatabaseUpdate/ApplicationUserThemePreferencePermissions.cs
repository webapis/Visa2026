using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EF;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Default-role member permissions for per-user theme preference fields on <see cref="ApplicationUser"/>.
/// </summary>
public static class ApplicationUserThemePreferencePermissions
{
    public const string DefaultRoleName = "Default";

    public static void EnsureDefaultRoleSelfWrite(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
        {
            return;
        }

        PermissionPolicyRole? defaultRole = objectSpace
            .FirstOrDefault<PermissionPolicyRole>(role => role.Name == DefaultRoleName);
        if (defaultRole == null)
        {
            return;
        }

        EnsureSelfWrite(defaultRole);
        objectSpace.CommitChanges();
    }

    public static void EnsureSelfWrite(PermissionPolicyRole defaultRole)
    {
        if (defaultRole == null)
        {
            return;
        }

        string[] memberNames =
        [
            nameof(ApplicationUser.PreferredThemeCaption),
            nameof(ApplicationUser.PreferredThemeMode),
            nameof(ApplicationUser.PreferredSizeMode)
        ];

        foreach (string memberName in memberNames)
        {
            bool alreadyGranted = defaultRole.TypePermissions
                .SelectMany(tp => tp.MemberPermissions)
                .Any(mp => string.Equals(mp.Members, memberName, StringComparison.Ordinal));
            if (alreadyGranted)
            {
                continue;
            }

            defaultRole.AddMemberPermissionFromLambda<ApplicationUser>(
                SecurityOperations.Write,
                memberName,
                cm => cm.ID == (Guid)CurrentUserIdOperator.CurrentUserId(),
                SecurityPermissionState.Allow);
        }
    }
}
