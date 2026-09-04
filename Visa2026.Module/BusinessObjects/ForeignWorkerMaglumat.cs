using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Read-only MAGLUMAT row: person with current work permit, passport, visa, education, address.
    /// Backed by View_ForeignWorkerMaglumat, which has no PostgreSQL creator yet
    /// (the SQL Server DDL was removed with ApplicationItem Phase B — see docs/DEPRECATED.md).
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("WorkPermit")]
    [DefaultProperty(nameof(FullName))]
    [ModelDefault("Caption", "Daşary ýurt raýatlary maglumaty")]
    [ModelDefault("AllowEdit", "False")]
    [ModelDefault("AllowNew", "False")]
    [ModelDefault("AllowDelete", "False")]
    [ListViewFilter("AllWorkPermits", "", "All (current WP)", true)]
    [ListViewFilter("ValidWorkPermits", "IsValid = True", "Valid work permits")]
    public class ForeignWorkerMaglumat
    {
        [Key]
        [Browsable(false)]
        public virtual Guid ID { get; set; }

        [Browsable(false)]
        public virtual Guid? PersonID { get; set; }

        [ModelDefault("Caption", "F.A.A")]
        public virtual string FullName { get; set; }

        [ModelDefault("Caption", "Doglan senesi")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? DateOfBirth { get; set; }

        [ModelDefault("Caption", "Raýatlygy")]
        public virtual string NationalityCode { get; set; }

        [ModelDefault("Caption", "Pasport belgisi")]
        public virtual string PassportNumber { get; set; }

        [ModelDefault("Caption", "Pasport möhleti")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? PassportExpirationDate { get; set; }

        [ModelDefault("Caption", "Bilimi")]
        public virtual string EducationLevelTm { get; set; }

        [ModelDefault("Caption", "Wezipesi")]
        public virtual string PositionNameTm { get; set; }

        [ModelDefault("Caption", "Ýaşaýan salgysy")]
        public virtual string ResidenceAddress { get; set; }

        [ModelDefault("Caption", "Rugsatnama belgisi")]
        public virtual string WorkPermitNumber { get; set; }

        [ModelDefault("Caption", "Rugsatnama başy")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? WorkPermitStartDate { get; set; }

        [ModelDefault("Caption", "Rugsatnama soňy")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? WorkPermitExpirationDate { get; set; }

        [ModelDefault("Caption", "Ýatyrylan")]
        public virtual bool WorkPermitIsCancelled { get; set; }

        [ModelDefault("Caption", "Dogry (valid)")]
        public virtual bool IsValid { get; set; }

        [ModelDefault("Caption", "Wiza belgisi")]
        public virtual string VisaNumber { get; set; }

        [ModelDefault("Caption", "Wiza başy")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? VisaStartDate { get; set; }

        [ModelDefault("Caption", "Wiza soňy")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? VisaExpirationDate { get; set; }

        [ModelDefault("Caption", "Bellik")]
        public virtual string Remarks { get; set; }

        [ModelDefault("Caption", "Doglan ýyly we raýatlygy")]
        public virtual string BirthAndNationality { get; set; }

        [ModelDefault("Caption", "Pasport belgisi we möhleti")]
        public virtual string PassportBlock { get; set; }

        [ModelDefault("Caption", "Rugsatnama belgisi we möhleti")]
        public virtual string PermitBlock { get; set; }

        [ModelDefault("Caption", "Wiza belgisi we möhleti")]
        public virtual string VisaBlock { get; set; }
    }
}