using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    public class RejectionItem : BaseObject
    {
        [RuleRequiredField]
        public virtual Rejection Rejection { get; set; }

        [RuleRequiredField]
        [DataSourceProperty("Rejection.AvailablePeople")]
        public virtual Person Person { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [DataSourceProperty("Rejection.AvailablePeople")]
        [Appearance("EmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Rejection.Application.IsForFamily", Context = "DetailView")]
        public virtual Employee Employee
        {
            get => Person as Employee;
            set => Person = value;
        }

        [NotMapped]
        [ImmediatePostData]
        [DataSourceProperty("Rejection.AvailablePeople")]
        [Appearance("FamilyMemberVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!Rejection.Application.IsForFamily", Context = "DetailView")]
        public virtual FamilyMember FamilyMember
        {
            get => Person as FamilyMember;
            set => Person = value;
        }

        public virtual string Reason { get; set; }

        [RuleFromBoolProperty("RejectionItem_PersonIsValid", DefaultContexts.Save, "The selected person is not part of the parent application.")]
        [Browsable(false)]
        public bool IsPersonValid
        {
            get
            {
                if (Person == null || Rejection?.Application == null) return true;
                return Rejection.Application.ApplicationItems.Any(ai => ai.Person?.ID == Person.ID);
            }
        }
    }
}