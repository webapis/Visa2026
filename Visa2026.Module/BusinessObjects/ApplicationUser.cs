using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultProperty(nameof(UserName))]
    [NavigationItem("Auth")]
    public class ApplicationUser : PermissionPolicyUser, ISecurityUserWithLoginInfo, ISecurityUserLockout
    {
        [Browsable(false)]
        public virtual int AccessFailedCount { get; set; }

        [Browsable(false)]
        public virtual DateTime LockoutEnd { get; set; }

        /// <summary>BCP-47 UI culture (e.g. en-US). Set via runtime language switcher; restored on logon.</summary>
        [Browsable(false)]
        [MaxLength(10)]
        public virtual string PreferredCulture { get; set; }

        /// <summary>Theme switcher item caption (e.g. Blue, Blazing Dark). Restored on logon.</summary>
        [Browsable(false)]
        [MaxLength(64)]
        public virtual string PreferredThemeCaption { get; set; }

        /// <summary>Fluent light/dark mode (Light or Dark). Null for classic themes.</summary>
        [Browsable(false)]
        [MaxLength(8)]
        public virtual string PreferredThemeMode { get; set; }

        /// <summary>Size mode from theme switcher (Standard or Compact).</summary>
        [Browsable(false)]
        [MaxLength(16)]
        public virtual string PreferredSizeMode { get; set; }

        [Browsable(false)]
        [NonCloneable]
        [DevExpress.ExpressApp.DC.Aggregated]
        public virtual IList<ApplicationUserLoginInfo> UserLogins { get; set; } = new ObservableCollection<ApplicationUserLoginInfo>();

        IEnumerable<ISecurityUserLoginInfo> IOAuthSecurityUser.UserLogins => UserLogins.OfType<ISecurityUserLoginInfo>();

        ISecurityUserLoginInfo ISecurityUserWithLoginInfo.CreateUserLoginInfo(string loginProviderName, string providerUserKey)
        {
            ApplicationUserLoginInfo result = ((IObjectSpaceLink)this).ObjectSpace.CreateObject<ApplicationUserLoginInfo>();
            result.LoginProviderName = loginProviderName;
            result.ProviderUserKey = providerUserKey;
            result.User = this;
            return result;
        }
    }
}
