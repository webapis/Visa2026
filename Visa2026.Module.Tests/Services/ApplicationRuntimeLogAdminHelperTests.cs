using System.Collections.ObjectModel;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationRuntimeLogAdminHelperTests
{
    [Fact]
    public void IsAdministratorUser_Null_ReturnsFalse()
    {
        Assert.False(ApplicationRuntimeLogAdminHelper.IsAdministratorUser(null));
    }

    [Fact]
    public void IsAdministratorUser_NoRoles_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Roles = new ObservableCollection<PermissionPolicyRole>()
        };

        Assert.False(ApplicationRuntimeLogAdminHelper.IsAdministratorUser(user));
    }

    [Fact]
    public void IsAdministratorUser_NonAdminRole_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Roles = new ObservableCollection<PermissionPolicyRole>
            {
                new PermissionPolicyRole { Name = "Users", IsAdministrative = false }
            }
        };

        Assert.False(ApplicationRuntimeLogAdminHelper.IsAdministratorUser(user));
    }

    [Fact]
    public void IsAdministratorUser_AdminRole_ReturnsTrue()
    {
        var user = new ApplicationUser
        {
            Roles = new ObservableCollection<PermissionPolicyRole>
            {
                new PermissionPolicyRole { Name = "Users", IsAdministrative = false },
                new PermissionPolicyRole { Name = "Administrators", IsAdministrative = true }
            }
        };

        Assert.True(ApplicationRuntimeLogAdminHelper.IsAdministratorUser(user));
    }
}
