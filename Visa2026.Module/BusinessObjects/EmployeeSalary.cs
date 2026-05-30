using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
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
    public class EmployeeSalary : BaseObject, IObjectSpaceLink, ICurrentPersonItem, ISoftDelete
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
        public virtual EmployeeCurrency Currency { get; set; }

        [ImmediatePostData]
        [Appearance("EmployeeSalary_DisableUncheckIsActive", Enabled = false, Criteria = "IsActive")]
        public virtual bool IsActive { get; set; }

        [NotMapped]
        [VisibleInListView(false)]
        public string Title => VisaUiMessages.Format(
            "EmployeeSalary.DisplayTitle",
            $"{Amount} {Currency}",
            StartDate.ToString("d", CultureInfo.CurrentUICulture));

        public override void OnCreated()
        {
            base.OnCreated();
            CurrentPersonItemSync.OnCreated(this);
            StartDate = DateTime.Today;
        }

        public override void OnSaving()
        {
            base.OnSaving();
            CurrentPersonItemSync.ApplyOnSaving(
                this,
                _ => Person,
                p => p.Salaries,
                _ => StartDate,
                item =>
                {
                    if (!item.IsActive || Person?.Salaries == null)
                        return;

                    foreach (var sibling in Person.Salaries)
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
