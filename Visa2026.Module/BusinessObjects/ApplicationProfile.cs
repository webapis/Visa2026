using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Reusable application configuration (replaces deprecated <see cref="ApplicationType"/>).
/// Applications hold a live FK; configuration-related fields apply to all linked Applications
/// until progress lock state A. See docs/APPLICATION_PROFILE_PLAN.md.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Configuration")]
[DefaultProperty(nameof(DisplayName))]
[XafDisplayName("Application Profile")]
[ImageName("BO_List")]
public class ApplicationProfile : BaseObject
{
    public ApplicationProfile()
    {
        ApprovalLegs = new ObservableCollection<ApplicationProfileApprovalLeg>();
        NestedTemplates = new ObservableCollection<ApplicationProfileTemplate>();
        Applications = new ObservableCollection<Application>();
    }

    [RuleRequiredField]
    [MaxLength(200)]
    [XafDisplayName("Application name")]
    public virtual string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    [XafDisplayName("Description")]
    public virtual string? Description { get; set; }

    [RuleRequiredField]
    [MaxLength(64)]
    [XafDisplayName("Code")]
    public virtual string Code { get; set; } = string.Empty;

    /// <summary>Optional 3-digit quick pick (legacy SelectionCode role).</summary>
    [MaxLength(3)]
    [XafDisplayName("Selection / quick code")]
    [RuleRegularExpression(
        "ApplicationProfileSelectionCodeFormat",
        DefaultContexts.Save,
        @"^(\d{3})?$",
        CustomMessageTemplate = "Selection code must be empty or exactly three digits.")]
    public virtual string? SelectionCode { get; set; }

    [ImmediatePostData]
    [XafDisplayName("Directed to")]
    public virtual ApplicationProgressRouteKind ProgressRoute { get; set; }
        = ApplicationProgressRouteKind.ViaMinistries;

    [XafDisplayName("For employee")]
    public virtual bool ForEmployee { get; set; } = true;

    [XafDisplayName("For family member")]
    public virtual bool ForFamilyMember { get; set; }

    [XafDisplayName("For temporary visitor")]
    public virtual bool ForTemporaryVisitor { get; set; }

    [ImmediatePostData]
    [XafDisplayName("Related to")]
    public virtual ApplicationProfileActionFamily ActionFamily { get; set; }
        = ApplicationProfileActionFamily.Issuance;

    // --- Produce / cancel (configuration-related, live) ---

    [XafDisplayName("May produce invitation")]
    public virtual bool ProduceInvitation { get; set; }

    [XafDisplayName("May produce work permit")]
    public virtual bool ProduceWorkPermit { get; set; }

    [XafDisplayName("May produce visa")]
    public virtual bool ProduceVisa { get; set; }

    [XafDisplayName("May produce border zone")]
    public virtual bool ProduceBorderZone { get; set; }

    [XafDisplayName("May produce work location")]
    public virtual bool ProduceWorkLocation { get; set; }

    [XafDisplayName("May cancel invitation(s)")]
    public virtual bool CancelInvitations { get; set; }

    [XafDisplayName("May cancel work permit(s)")]
    public virtual bool CancelWorkPermits { get; set; }

    [XafDisplayName("May cancel visa(s)")]
    public virtual bool CancelVisas { get; set; }

    [XafDisplayName("May cancel border zone permit(s)")]
    public virtual bool CancelBorderZonePermits { get; set; }

    [XafDisplayName("May cancel application(s)")]
    public virtual bool CancelApplications { get; set; }

    // --- Per-application field catalog: required + defaults ---

    public virtual bool RequireVisaType { get; set; }
    public virtual VisaType? DefaultVisaType { get; set; }
    public virtual Guid? DefaultVisaTypeId { get; set; }

    public virtual bool RequireVisaCategory { get; set; }
    public virtual VisaCategory? DefaultVisaCategory { get; set; }
    public virtual Guid? DefaultVisaCategoryId { get; set; }

    public virtual bool RequireVisaPeriod { get; set; }
    public virtual VisaPeriod? DefaultVisaPeriod { get; set; }
    public virtual Guid? DefaultVisaPeriodId { get; set; }

    public virtual bool RequireBorderZone { get; set; }
    [MaxLength(500)]
    public virtual string? DefaultBorderZoneLocation { get; set; }

    public virtual bool RequireMigrationService { get; set; }
    public virtual MigrationService? DefaultMigrationService { get; set; }
    public virtual Guid? DefaultMigrationServiceId { get; set; }

    public virtual bool RequireStartDate { get; set; }
    public virtual bool RequireEndDate { get; set; }
    public virtual bool RequireRegionCity { get; set; }
    public virtual bool RequireBusinessTripAddress { get; set; }

    public virtual bool RequireProject { get; set; }
    public virtual ProjectContract? DefaultProjectContract { get; set; }
    public virtual Guid? DefaultProjectContractId { get; set; }

    public virtual bool RequireUrgency { get; set; }
    public virtual Urgency? DefaultUrgency { get; set; }
    public virtual Guid? DefaultUrgencyId { get; set; }

    public virtual bool RequireWorkPermitLocation { get; set; }
    public virtual bool RequireEntryDate { get; set; }

    public virtual bool RequireEntryCheckPoint { get; set; }
    public virtual CheckPoint? DefaultEntryCheckPoint { get; set; }
    public virtual Guid? DefaultEntryCheckPointId { get; set; }

    // --- Signatory defaults (per-Application values seeded at create) ---

    public virtual AuthorizedSignatory? DefaultAuthorizedSignatory { get; set; }
    public virtual Guid? DefaultAuthorizedSignatoryId { get; set; }

    public virtual AuthorizedRepresentative? DefaultVisaRepresentative { get; set; }
    public virtual Guid? DefaultVisaRepresentativeId { get; set; }

    // --- Process ---

    [XafDisplayName("Ministry SLA (days)")]
    public virtual int MinistrySlaDays { get; set; } = 14;

    [XafDisplayName("Migration SLA (days)")]
    public virtual int MigrationSlaDays { get; set; } = 14;

    [Aggregated]
    [InverseProperty(nameof(ApplicationProfileApprovalLeg.ApplicationProfile))]
    [XafDisplayName("Approval legs")]
    public virtual IList<ApplicationProfileApprovalLeg> ApprovalLegs { get; set; }

    // --- Person-config toggles (tab visibility / requirements) ---

    public virtual bool RequirePersonPassport { get; set; } = true;
    public virtual bool RequirePersonEducation { get; set; }
    public virtual bool RequirePersonPosition { get; set; }
    public virtual bool RequirePersonAddressOfResidence { get; set; }
    public virtual bool RequirePersonVisa { get; set; }
    public virtual bool RequirePersonInvitationItem { get; set; }
    public virtual bool RequirePersonWorkPermitItem { get; set; }
    public virtual bool RequirePersonBorderZoneItem { get; set; }
    public virtual bool RequirePersonSalary { get; set; }
    public virtual bool RequirePersonMedical { get; set; }
    public virtual bool RequirePersonRejectionItem { get; set; }
    public virtual bool RequirePersonTravelHistory { get; set; }

    // --- Templates ---

    [Aggregated]
    [InverseProperty(nameof(ApplicationProfileTemplate.ApplicationProfile))]
    [XafDisplayName("Nested templates")]
    public virtual IList<ApplicationProfileTemplate> NestedTemplates { get; set; }

    /// <summary>Freeform XAF criteria filtering when this profile appears in pickers.</summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    [XafDisplayName("Applicability criteria")]
    [ToolTip("Optional. When empty, profile is available subject to audience/route rules. Criteria target Application context.")]
    public virtual string? ApplicabilityCriteria { get; set; }

    public virtual bool IsActive { get; set; } = true;

    [InverseProperty(nameof(Application.ApplicationProfile))]
    [VisibleInDetailView(false)]
    public virtual IList<Application> Applications { get; set; }

    [NotMapped]
    [VisibleInDetailView(false)]
    [VisibleInListView(true)]
    public string DisplayName =>
        string.IsNullOrWhiteSpace(SelectionCode)
            ? Name
            : $"{SelectionCode} · {Name}";

    /// <summary>
    /// True when any linked Application has left office preparation / been submitted
    /// (lock state A). Used to make the profile wizard read-only.
    /// </summary>
    [NotMapped]
    [VisibleInListView(true)]
    [XafDisplayName("Config locked")]
    public bool IsConfigLocked =>
        Applications?.Any(a => ApplicationProfileLockHelper.IsApplicationAtOrPastLockStateA(a)) == true;

    public override string ToString() => DisplayName;
}

/// <summary>Exclusive “Application related to” family on <see cref="ApplicationProfile"/>.</summary>
public enum ApplicationProfileActionFamily
{
    Issuance = 0,
    Cancellation = 1,
    Registration = 2,
    BusinessTrip = 3
}

/// <summary>Ordered ministry approval leg embedded on <see cref="ApplicationProfile"/>.</summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(Sequence))]
public class ApplicationProfileApprovalLeg : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationProfileId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationProfileId))]
    public virtual ApplicationProfile ApplicationProfile { get; set; } = null!;

    [RuleRequiredField]
    public virtual int? Sequence { get; set; }

    [RuleRequiredField]
    public virtual ApprovingMinistry? ApprovingMinistry { get; set; }

    [Browsable(false)]
    public virtual Guid? ApprovingMinistryId { get; set; }
}

/// <summary>Word/Excel/PDF template file nested on <see cref="ApplicationProfile"/>.</summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(TemplateName))]
[FileAttachment(nameof(TemplateFile))]
public class ApplicationProfileTemplate : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationProfileId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationProfileId))]
    public virtual ApplicationProfile ApplicationProfile { get; set; } = null!;

    [RuleRequiredField]
    [MaxLength(255)]
    public virtual string TemplateName { get; set; } = string.Empty;

    [XafDisplayName("Template type")]
    public virtual ApplicationProfileTemplateKind TemplateKind { get; set; }
        = ApplicationProfileTemplateKind.Word;

    [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
    [XafDisplayName("Template file")]
    public virtual FileData? TemplateFile { get; set; }

    public virtual int SortOrder { get; set; }
}

public enum ApplicationProfileTemplateKind
{
    Word = 0,
    Excel = 1,
    PdfForm = 2
}

/// <summary>Progress lock state A helpers for <see cref="ApplicationProfile.IsConfigLocked"/>.</summary>
public static class ApplicationProfileLockHelper
{
    /// <summary>
    /// Lock when progress has left initial office preparation (submitted to ministry or migration, or later).
    /// </summary>
    public static bool IsApplicationAtOrPastLockStateA(Application? application)
    {
        if (application == null)
            return false;

        var code = application.LatestPrimaryStateCode
            ?? application.LatestProgress?.State?.Code;

        if (string.IsNullOrWhiteSpace(code))
            return false;

        // Initial / pre-submit office work — not locked yet.
        if (code.Equals("OFFICE_PREPARATION", StringComparison.OrdinalIgnoreCase)
            || code.Equals("DRAFT", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
