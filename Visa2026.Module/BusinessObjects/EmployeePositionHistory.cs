using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Title))]
    public class EmployeePositionHistory : BaseObject, IObjectSpaceLink, ICurrentPersonItem, ISoftDelete
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

        [ImmediatePostData]
        [Appearance("EmployeePositionHistory_DisableUncheckIsActive", Enabled = false, Criteria = "IsActive")]
        public virtual bool IsActive { get; set; }

        [NotMapped]
        [VisibleInListView(false)]
        public string Title => VisaUiMessages.Format(
            "PositionHistory.DisplayTitle",
            Position?.NameTm ?? string.Empty,
            StartDate.ToString("d", CultureInfo.CurrentUICulture));

        public override void OnCreated()
        {
            base.OnCreated();
            CurrentPersonItemSync.OnCreated(this);
        }

        public override void OnSaving()
        {
            base.OnSaving();
            CurrentPersonItemSync.ApplyOnSaving(
                this,
                _ => Person,
                p => p.PositionHistory,
                _ => StartDate,
                item =>
                {
                    if (!item.IsActive || Person?.PositionHistory == null)
                        return;

                    foreach (var sibling in Person.PositionHistory)
                    {
                        if (ReferenceEquals(sibling, item))
                            continue;
                        if (sibling is ISoftDelete sd && sd.IsDeleted)
                            continue;
                        if (sibling.IsActive)
                            sibling.EndDate = item.StartDate;
                    }
                });
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}
