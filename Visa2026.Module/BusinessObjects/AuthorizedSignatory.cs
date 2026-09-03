using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem(false)]
    [DisplayName("Authorized Signatory")]
    [DefaultProperty(nameof(FullName))]
    [ImageName("BO_Position")]
    public class AuthorizedSignatory : BaseObject
    {
        [RuleRequiredField(DefaultContexts.Save)]
        [XafDisplayName("Full Name")]
        public virtual string FullName { get; set; }

        [XafDisplayName("Position (Tm)")]
        public virtual string PositionTitleTm { get; set; }

        [XafDisplayName("Passport Number")]
        public virtual string PassportNumber { get; set; }

        public virtual string PassportAuthority { get; set; }

        public virtual DateTime? PassportIssueDate { get; set; }

        [XafDisplayName("Passport Expiration Date")]
        public virtual DateTime? PassportExpirationDate { get; set; }

        [XafDisplayName("Default")]
        [ToolTip("Pre-selected when creating the next Application Profile Instance.")]
        public virtual bool IsDefault { get; set; }

        [NotMapped]
        [XafDisplayName("Passport (one line)")]
        public string PassportLine =>
            OrganizationPassportLineHelper.Format(PassportNumber, PassportAuthority, PassportIssueDate);

        public static AuthorizedSignatory? TryGetInstance(IObjectSpace objectSpace) =>
            OrganizationCatalogHelper.TryGetDefaultSignatory(objectSpace);

        public static AuthorizedSignatory GetOrCreateInstance(IObjectSpace objectSpace) =>
            TryGetInstance(objectSpace) ?? objectSpace.CreateObject<AuthorizedSignatory>();
    }
}
