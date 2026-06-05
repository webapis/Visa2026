using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Editors;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Title))]
    [SupportsOptionalDetailFields]
    public class EmployeeSalary : BaseObject, IOptionalDetailFields
    {
        [Index(0)]
        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person { get; set; }

        [Index(1)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime StartDate { get; set; }

        [Index(2)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? EndDate { get; set; }

        [Index(3)]
        [RuleRequiredField]
        [MaxLength(32)]
        public virtual string Amount { get; set; }

        [Index(4)]
        [RuleRequiredField]
        public virtual EmployeeCurrency? Currency { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        [XafDisplayName(" ")]
        public bool ShowOptionalFields { get; set; }

        [NotMapped]
        [VisibleInListView(false)]
        public string Title => VisaUiMessages.Format(
            "EmployeeSalary.DisplayTitle",
            $"{Amount} {Currency}");

        public override void OnCreated()
        {
            base.OnCreated();
            StartDate = DateTime.Today;
        }

        public override void OnSaving()
        {
            base.OnSaving();
            if (Person?.Salaries == null)
                return;

            foreach (var sibling in Person.Salaries)
            {
                if (ReferenceEquals(sibling, this))
                    continue;
                if ((sibling.EndDate == null || sibling.EndDate.Value.Date >= DateTime.Today)
                    && StartDate > sibling.StartDate)
                    sibling.EndDate = StartDate;
            }
        }

    }
}
