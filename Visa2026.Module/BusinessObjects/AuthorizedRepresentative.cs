using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Organization")]
    [DisplayName("Authorized Representative")]
    [DefaultProperty(nameof(FullName))]
    [ImageName("BO_Contact")]
    public class AuthorizedRepresentative : BaseObject
    {
        [RuleRequiredField(DefaultContexts.Save)]
        [XafDisplayName("Full Name")]
        public virtual string FullName { get; set; }

        [XafDisplayName("Position (Tm)")]
        public virtual string PositionTitleTm { get; set; }

        public virtual string Phone { get; set; }

        [XafDisplayName("Passport Number")]
        public virtual string PassportNumber { get; set; }

        public virtual string PassportAuthority { get; set; }

        public virtual DateTime? PassportIssueDate { get; set; }

        [NotMapped]
        [XafDisplayName("Passport (one line)")]
        public string PassportLine =>
            OrganizationPassportLineHelper.Format(PassportNumber, PassportAuthority, PassportIssueDate);

        public static AuthorizedRepresentative? TryGetInstance(IObjectSpace objectSpace) =>
            objectSpace.GetObjectsQuery<AuthorizedRepresentative>().FirstOrDefault();

        public static AuthorizedRepresentative GetOrCreateInstance(IObjectSpace objectSpace) =>
            TryGetInstance(objectSpace) ?? objectSpace.CreateObject<AuthorizedRepresentative>();
    }
}
