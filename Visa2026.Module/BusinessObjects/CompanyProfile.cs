using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Services;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Documentation;

namespace Visa2026.Module.BusinessObjects
{
    [UserDocumentation("administration/configuration/organization", Category = "Administration")]
    [DefaultClassOptions]
    [NavigationItem(false)]
    [DisplayName("Company")]
    [ImageName("BO_Organization")]
    public class CompanyProfile : BaseObject
    {
        [RuleRequiredField(DefaultContexts.Save)]
        public virtual string Name { get; set; }

        [MaxLength(10)]
        [XafDisplayName("Code")]
        [ToolTip("Letterhead / report asset key (e.g. background_CLK.jpg).")]
        public virtual string Code { get; set; }

        public virtual string Address { get; set; }

        public virtual string PhoneNumber { get; set; }

        public virtual string Email { get; set; }

        public virtual string TaxInformation { get; set; }

        [XafDisplayName("Company Registration Date")]
        public virtual DateTime? RegistrationDate { get; set; }

        [NotMapped]
        [XafDisplayName("Company Registration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string RegistrationDateText =>
            RegistrationDate is { } date && date.Year > 1 ? date.ToString("dd.MM.yyyy") : string.Empty;

        public static CompanyProfile? TryGetInstance(IObjectSpace objectSpace) =>
            OrganizationSingletonHelper.TryGet(objectSpace, (CompanyProfile p) => p.Name);

        public static CompanyProfile GetOrCreateInstance(IObjectSpace objectSpace) =>
            TryGetInstance(objectSpace) ?? objectSpace.CreateObject<CompanyProfile>();
    }
}
