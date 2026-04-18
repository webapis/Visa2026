using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(DefaultProperty))]
    public class BusinessTrip : SingleActiveBaseObject<Person, BusinessTrip>,ISoftDelete
    {
        [NotMapped]
        [Browsable(false)]
        public string DefaultProperty => Person?.FullName;

        private Person person;
        [RuleRequiredField]
        [ImmediatePostData]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    if (person != null)
                    {
                        CurrentPassport = person.CurrentPassport;
                        CurrentVisa = person.CurrentVisa;
                        CurrentAddressOfResidence = person.CurrentAddressOfResidence;
                        CurrentPositionHistory = person.CurrentPositionHistory;
                    }
                }
            }
        }

        public virtual Application Application { get; set; }

        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }

        public virtual Passport CurrentPassport { get; set; }

        public virtual Visa CurrentVisa { get; set; }

        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        [Aggregated]
        public virtual BusinessTripAddress BusinessTripAddress { get; set; }

        #region Report [NotMapped] properties — Sanawy report fields

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_LastName => Person?.LastName;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_FirstName => Person?.FirstName;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_DateOfBirthText => Person?.DateOfBirth is DateTime dob ? dob.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_BirthPlace => Person?.BirthPlace;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_GenderTm => Person?.Gender?.NameTm;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_NationalityCode => Person?.Nationality?.Code;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_Number => CurrentPassport?.PassportNumber;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_ExpirationDateText => CurrentPassport?.ExpirationDate is DateTime exp ? exp.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_NameTm => CurrentPositionHistory?.Position?.NameTm;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_NumberAndType => string.Join(" ", new[] { CurrentVisa?.VisaNumber, CurrentVisa?.VisaCategory?.NameTm }.Where(s => !string.IsNullOrEmpty(s)));

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_StartDateText => CurrentVisa != null ? CurrentVisa.StartDate.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_ExpirationDateText => CurrentVisa?.ExpirationDate is DateTime visaExp ? visaExp.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_FullAddress => CurrentAddressOfResidence?.FullAddress;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string BusinessTripAddress_FullAddress => BusinessTripAddress?.FullAddress;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyHead_FullName => Application?.CompanyHead?.FullName;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyHead_PositionTm => Application?.CompanyHead?.Position?.NameTm;

        #endregion

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<BusinessTrip> GetSiblings(Person parent)
        {
            return parent?.BusinessTrips;
        }

        public override void SetParentActiveItem(Person parent, BusinessTrip item)
        {
            parent.CurrentBusinessTrip = item;
        }

        public override bool IsParentActiveItem(Person parent, BusinessTrip item)
        {
            return parent.CurrentBusinessTrip == item;
        }

              [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}