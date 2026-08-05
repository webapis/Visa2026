using System.Linq;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.ReportDashboard;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Users / UsersReadOnly / VisaOffice: Report Dashboard host + all <c>vw_rd_*</c> view BOs.
/// Upgrades existing permission rows (Read + Navigate) — <see cref="Updater.EnsureTypePermission{T}"/> only adds missing types.
/// </summary>
public static class ReportDashboardOfficerPermissions
{
    public static void Ensure(PermissionPolicyRole role)
    {
        if (role == null)
        {
            return;
        }

        EnsureHostShellPermission(role);

        EnsureViewReadPermission<VwRdProject>(role);
        EnsureViewReadPermission<VwRdPersonRole>(role);
        EnsureViewReadPermission<VwRdApplication>(role);

        EnsureViewReadPermission<VwRdApplicationViaMinistryInvitationOnProcess>(role);
        EnsureViewReadPermission<VwRdApplicationViaMinistryInvitationOnProcessByPeriodCategoryType>(role);
        EnsureViewReadPermission<VwRdApplicationViaMinistryInvitationCompleted>(role);
        EnsureViewReadPermission<VwRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType>(role);
        EnsureViewReadPermission<VwRdApplicationViaMinistryVisaExtensionOnProcess>(role);
        EnsureViewReadPermission<VwRdApplicationViaMinistryVisaExtensionOnProcessByPeriodCategoryType>(role);
        EnsureViewReadPermission<VwRdApplicationViaMinistryVisaExtensionCompleted>(role);
        EnsureViewReadPermission<VwRdApplicationViaMinistryVisaExtensionCompletedByPeriodCategoryType>(role);
        EnsureViewReadPermission<VwRdApplicationViaMinistryOtherOnProcess>(role);
        EnsureViewReadPermission<VwRdApplicationViaMinistryOtherCompleted>(role);
        EnsureViewReadPermission<VwRdApplicationDirectMigrationOnProcessA>(role);
        EnsureViewReadPermission<VwRdApplicationDirectMigrationProcessComplete>(role);

        EnsureViewReadPermission<VwRdVisaAppProgress>(role);
        EnsureViewReadPermission<VwRdVisaByPeriod>(role);
        EnsureViewReadPermission<VwRdVisaByCategory>(role);
        EnsureViewReadPermission<VwRdVisaByType>(role);
        EnsureViewReadPermission<VwRdVisaState>(role);
        EnsureViewReadPermission<VwRdVisaActiveByProject>(role);
        EnsureViewReadPermission<VwRdVisaActiveByPeriodCategoryType>(role);
        EnsureViewReadPermission<VwRdVisaOnExtension>(role);
        EnsureViewReadPermission<VwRdVisaOnExtensionByPeriodCategoryType>(role);
        EnsureViewReadPermission<VwRdVisaExtensionResult>(role);
        EnsureViewReadPermission<VwRdVisaExtensionResultByPeriodCategoryType>(role);
        EnsureViewReadPermission<VwRdVisaExtensionRequired>(role);
        EnsureViewReadPermission<VwRdVisaByDaysRemaining>(role);

        EnsureViewReadPermission<VwRdInvitationReady>(role);
        EnsureViewReadPermission<VwRdInvitationInProcess>(role);
        EnsureViewReadPermission<VwRdInvitationUsed>(role);
        EnsureViewReadPermission<VwRdInvitationValidUntil>(role);
        EnsureViewReadPermission<VwRdInvitationRejected>(role);

        EnsureViewReadPermission<VwRdRegistration>(role);
        EnsureViewReadPermission<VwRdToBeCheckedIn>(role);
        EnsureViewReadPermission<VwRdToBeCheckedOut>(role);

        EnsureViewReadPermission<VwRdWorkPermit>(role);
        EnsureViewReadPermission<VwRdWorkPermitActive>(role);
        EnsureViewReadPermission<VwRdWorkPermitAppProgress>(role);

        EnsureViewReadPermission<VwRdPassport>(role);
        EnsureViewReadPermission<VwRdEducation>(role);
        EnsureViewReadPermission<VwRdEducationByCountry>(role);
        EnsureViewReadPermission<VwRdPositionHistory>(role);

        EnsureViewReadPermission<VwRdIncompletePersonsByMissingArea>(role);
        EnsureViewReadPermission<VwRdPersonSearch>(role);
    }

    private static void EnsureHostShellPermission(PermissionPolicyRole role)
    {
        var targetType = typeof(ReportDashboardHost);
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
        role.AddTypePermission<ReportDashboardHost>(readWriteCreate, SecurityPermissionState.Allow);
        var newPerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
        if (newPerm != null)
        {
            newPerm.NavigateState = SecurityPermissionState.Allow;
        }
    }

    /// <summary>SQL views are read-only; officers need Read + Navigate (overview, preview, ListView, Excel).</summary>
    private static void EnsureViewReadPermission<T>(PermissionPolicyRole role) where T : class
    {
        var targetType = typeof(T);
        var existingPerm = role.TypePermissions.FirstOrDefault(p => p.TargetType == targetType);
        if (existingPerm != null)
        {
            existingPerm.ReadState = SecurityPermissionState.Allow;
            existingPerm.NavigateState = SecurityPermissionState.Allow;
            existingPerm.WriteState = SecurityPermissionState.Deny;
            existingPerm.CreateState = SecurityPermissionState.Deny;
            existingPerm.DeleteState = SecurityPermissionState.Deny;
            return;
        }

        role.AddTypePermissionsRecursively<T>(SecurityOperations.Read, SecurityPermissionState.Allow);
        var newPerm = role.TypePermissions.First(p => p.TargetType == targetType);
        newPerm.NavigateState = SecurityPermissionState.Allow;
        newPerm.WriteState = SecurityPermissionState.Deny;
        newPerm.CreateState = SecurityPermissionState.Deny;
        newPerm.DeleteState = SecurityPermissionState.Deny;
    }
}
