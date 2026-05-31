using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
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
    public class EmployeePositionHistory : BaseObject, ISoftDelete, IOptionalDetailFields
    {
        [Index(1)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime StartDate { get; set; }

        [Index(2)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? EndDate { get; set; }

        [Index(0)]
        [RuleRequiredField]
        [XafDisplayName("Position (visa reports)")]
        public virtual Position Position { get; set; }

        [RuleRequiredField]
        [Index(4)]
        [XafDisplayName("Position (actual / company)")]
        public virtual ActualPosition ActualPosition { get; set; }

        //[RuleRequiredField]
        [VisibleInListView(false)]
        public virtual Department Department { get; set; }

        [RuleRequiredField]
        [Index(3)]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person { get; set; }

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
            "PositionHistory.DisplayTitle",
            Position?.NameTm ?? string.Empty,
            StartDate.ToString("d", CultureInfo.CurrentUICulture));

        public override void OnCreated()
        {
            base.OnCreated();
        }

        public override void OnSaving()
        {
            base.OnSaving();
            if (Person?.PositionHistory == null)
                return;

            foreach (var sibling in Person.PositionHistory)
            {
                if (ReferenceEquals(sibling, this))
                    continue;
                if (sibling is ISoftDelete sd && sd.IsDeleted)
                    continue;
                if ((sibling.EndDate == null || sibling.EndDate.Value.Date >= DateTime.Today)
                    && StartDate > sibling.StartDate)
                    sibling.EndDate = StartDate;
            }
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}
