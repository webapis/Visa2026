using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(FullName))]
    [DisplayName("Authorized Signatory")]
    [RuleCriteria("CompanyHeadSource", DefaultContexts.Save, "LocalEmployee is not null or Employee is not null", "Please select a Local Employee or an Expat Employee.")]
    public class CompanyHead : SingleActiveBaseObject<Company, CompanyHead>
    {
        public CompanyHead()
        {
            Images = new ObservableCollection<CompanyHeadImage>();
            Documents = new ObservableCollection<CompanyHeadDocument>();
        }

        public virtual Company Company { get; set; }

        private bool isLocalEmployee;
        [ImmediatePostData]
        public virtual bool IsLocalEmployee
        {
            get => isLocalEmployee;
            set
            {
                if (isLocalEmployee != value)
                {
                    isLocalEmployee = value;
                    if (isLocalEmployee) Employee = null;
                    else LocalEmployee = null;
                }
            }
        }

        [DataSourceCriteria("Company = '@This.Company'")]
        [Appearance("CompanyHeadLocalEmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!IsLocalEmployee", Context = "DetailView")]
        public virtual LocalEmployee LocalEmployee { get; set; }

        [Appearance("CompanyHeadEmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "IsLocalEmployee", Context = "DetailView")]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Employee { get; set; }

        [NotMapped]
        public string FullName
        {
            get
            {
                if (LocalEmployee != null) return LocalEmployee.FullName;
                if (Employee != null) return Employee.FullName;
                return string.Empty;
            }
        }

        public virtual Position Position { get; set; }

        [Aggregated]
        [InverseProperty(nameof(CompanyHeadImage.CompanyHead))]
        public virtual IList<CompanyHeadImage> Images { get; set; }

        [Aggregated]
        [InverseProperty(nameof(CompanyHeadDocument.CompanyHead))]
        public virtual IList<CompanyHeadDocument> Documents { get; set; }

        public override Company GetParent()
        {
            return Company;
        }

        public override IList<CompanyHead> GetSiblings(Company parent)
        {
            return parent?.Heads;
        }

        public override void SetParentActiveItem(Company parent, CompanyHead item)
        {
            parent.CurrentAuthorizedSignatory = item;
        }

        public override bool IsParentActiveItem(Company parent, CompanyHead item)
        {
            return parent.CurrentAuthorizedSignatory == item;
        }
    }
}