using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(FullName))]
    [DisplayName("Authorized Representative")]
    [RuleCriteria("RepresentativeSource", DefaultContexts.Save, "LocalEmployee is not null or Employee is not null", "Please select a Local Employee or an Expat Employee.")]
    public class Representative : SingleActiveBaseObject<Company, Representative>
    {
        public Representative()
        {
            Images = new ObservableCollection<RepresentativeImage>();
            Documents = new ObservableCollection<RepresentativeDocument>();
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
        [Appearance("RepresentativeLocalEmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!IsLocalEmployee", Context = "DetailView")]
        public virtual LocalEmployee LocalEmployee { get; set; }

        [Appearance("RepresentativeEmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "IsLocalEmployee", Context = "DetailView")]
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

        [Aggregated]
        [InverseProperty(nameof(RepresentativeImage.Representative))]
        public virtual IList<RepresentativeImage> Images { get; set; }

        [Aggregated]
        [InverseProperty(nameof(RepresentativeDocument.Representative))]
        public virtual IList<RepresentativeDocument> Documents { get; set; }

        public override Company GetParent()
        {
            return Company;
        }

        public override IList<Representative> GetSiblings(Company parent)
        {
            return parent?.Representatives;
        }

        public override void SetParentActiveItem(Company parent, Representative item)
        {
            parent.CurrentRepresentative = item;
        }

        public override bool IsParentActiveItem(Company parent, Representative item)
        {
            return parent.CurrentRepresentative == item;
        }
    }
}