using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    [DefaultProperty(nameof(RegistrationName))]
    public class Registration : SingleActiveBaseObject<Person, Registration>,ISoftDelete
    {
        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        public virtual DateTime RegistrationDate { get; set; }

        public virtual DateTime? ExpirationDate { get; set; }

        [MaxLength(50)]
        public virtual string RegistrationNumber { get; set; }

        public virtual Application Application { get; set; }

        [ExpandObjectMembers(ExpandObjectMembers.InDetailView)]
        [Appearance("ShowTravelForSpecificApps", Visibility = ViewItemVisibility.Hide, Criteria = "Not Application.ApplicationType.Name In ('Visa Entry', 'Border Registration', 'App_Reg_Check_In', 'App_Reg_Check_Out', 'App_Reg_Check_Out_Internal', 'App_Reg_Check_In_Internal')", Context = "DetailView")]
        public virtual TravelHistory MovementRecord { get; set; }

        [NotMapped]
        public string RegistrationName => $"{Person?.FullName} - {Application?.ApplicationType?.Name} on {RegistrationDate:d}";

        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        [DisplayName("Nationality")]
        public string PersonNationality => Person?.Nationality?.Name;

        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        [DisplayName("Date of Birth")]
        public DateTime? PersonDateOfBirth => Person?.DateOfBirth;

        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        [DisplayName("Current Passport No.")]
        public string PersonPassportNumber => Person?.CurrentPassport?.PassportNumber;

        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        [DisplayName("Current Visa No.")]
        public string PersonVisaNumber => Person?.CurrentVisa?.VisaNumber;

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.RegistrationDate;

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<Registration> GetSiblings(Person parent)
        {
            return parent?.Registrations;
        }

        public override void SetParentActiveItem(Person parent, Registration item)
        {
            parent.CurrentRegistration = item;
        }

        public override bool IsParentActiveItem(Person parent, Registration item)
        {
            return parent.CurrentRegistration == item;
        }

              [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}