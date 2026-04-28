using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System.Linq;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]
    public class Company : BaseObject, IObjectSpaceLink
    {
        public Company()
        {
            Heads = new ObservableCollection<CompanyHead>();
            Representatives = new ObservableCollection<Representative>();
            LocalEmployees = new ObservableCollection<LocalEmployee>();
            Employees = new ObservableCollection<Person>();
            Images = new ObservableCollection<CompanyImage>();
            Documents = new ObservableCollection<CompanyDocument>();
            ProjectContracts = new ObservableCollection<ProjectContract>();
        }

        [RuleRequiredField(DefaultContexts.Save)]
        public virtual string Name { get; set; }

        [MaxLength(10)]
        [XafDisplayName("Code")]
        public virtual string Code { get; set; }

        public virtual string Address { get; set; }

        public virtual string PhoneNumber { get; set; }

        public virtual string Email { get; set; }

        public virtual string TaxInformation { get; set; }

        public virtual string AppNumberPrefix { get; set; }

        [DefaultValue(4)]
        public virtual int ApplicationNumberPadding { get; set; }

        [XafDisplayName("App Number Format")]
        [ToolTip("Tokens: {PREFIX} {YEAR} {YEAR2} {MONTH} {MONTH2} {NUMBER}. Example: {PREFIX}{YEAR}-{NUMBER} → TRM-2026-001, or №{MONTH}/-{NUMBER} → №3/-377")]
        public virtual string AppNumberFormat { get; set; }

        [XafDisplayName("App Number Seed")]
        [ToolTip("The last application number used before this system was deployed. New numbers will continue from this value. Example: if the last external number was 1150, set this to 1150 and the first generated number will be 1151.")]
        [DefaultValue(0)]
        public virtual int ApplicationNumberSeed { get; set; }

        public virtual bool IsDefault { get; set; }

        [Aggregated]
        [InverseProperty(nameof(CompanyHead.Company))]
        public virtual IList<CompanyHead> Heads { get; set; }

        public virtual CompanyHead CurrentAuthorizedSignatory { get; set; }

        [Aggregated]
        [InverseProperty(nameof(Representative.Company))]
        public virtual IList<Representative> Representatives { get; set; }

        public virtual Representative CurrentRepresentative { get; set; }

        [InverseProperty(nameof(LocalEmployee.Company))]
        public virtual IList<LocalEmployee> LocalEmployees { get; set; }

        [InverseProperty(nameof(Person.Company))]
        public virtual IList<Person> Employees { get; set; }

        [Aggregated]
        [InverseProperty(nameof(ProjectContract.Company))]
        public virtual IList<ProjectContract> ProjectContracts { get; set; }

        [Aggregated]
        [InverseProperty(nameof(CompanyImage.Company))]
        public virtual IList<CompanyImage> Images { get; set; }

        [Aggregated]
        [InverseProperty(nameof(CompanyDocument.Company))]
        public virtual IList<CompanyDocument> Documents { get; set; }

        public override void OnSaving()
        {
            base.OnSaving();

            // If this company is being set as the default,
            // ensure no other company is marked as default.
            if (ObjectSpace != null && IsDefault)
            {
                var otherDefaults = ObjectSpace.GetObjectsQuery<Company>().Where(c => c.ID != this.ID && c.IsDefault).ToList();
                foreach (var otherCompany in otherDefaults)
                {
                    otherCompany.IsDefault = false;
                }
            }
        }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}