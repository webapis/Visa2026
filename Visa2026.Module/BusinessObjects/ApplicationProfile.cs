using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Model;
using Microsoft.EntityFrameworkCore;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Localization;
using Visa2026.Module.Documentation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Reusable application configuration (replaces deprecated <see cref="ApplicationType"/>).
/// Applications hold a live FK; configuration-related fields apply to all linked Applications
/// until progress lock state A. See docs/APPLICATION_PROFILE_PLAN.md.
/// </summary>
[UserDocumentation("administration/configuration/application-profiles", Category = "Configuration")]
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(DisplayName))]
[XafDisplayName("Application Profile")]
[ImageName("BO_List")]
[Appearance(
    "ApplicationProfileConfigLockedReadOnly",
    AppearanceItemType = "ViewItem",
    TargetItems = "*",
    Criteria = "IsConfigLocked",
    Enabled = false,
    Context = "DetailView")]
public class ApplicationProfile : BaseObject
{
    public ApplicationProfile()
    {
        ApprovalLegs = new ObservableCollection<ApplicationProfileApprovalLeg>();
        ApprovalLegVersions = new ObservableCollection<ApplicationProfileApprovalLegVersion>();
        NestedTemplates = new ObservableCollection<ApplicationProfileTemplate>();
        ProgressStateSettings = new ObservableCollection<ApplicationProfileProgressStateSetting>();
        Instances = new ObservableCollection<ApplicationProfileInstance>();
    }

    [RuleRequiredField]
    [MaxLength(200)]
    [XafDisplayName("ApplicationProfileInstance name")]
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
    public virtual ApplicationProfileInstanceProgressRouteKind ProgressRoute { get; set; }
        = ApplicationProfileInstanceProgressRouteKind.ViaMinistries;

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

    /// <summary>
    /// When <see cref="ActionFamily"/> is Registration: Check in, Check out, Info change, or Reg extension.
    /// Report Dashboard will filter registration queries on this value.
    /// </summary>
    [ImmediatePostData]
    [XafDisplayName("Registration is")]
    public virtual ApplicationProfileRegistrationKind RegistrationKind { get; set; }
        = ApplicationProfileRegistrationKind.None;

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

    [XafDisplayName("May produce rejection")]
    public virtual bool ProduceRejection { get; set; }

    [XafDisplayName("May cancel invitation(s)")]
    public virtual bool CancelInvitations { get; set; }

    [XafDisplayName("May cancel work permit(s)")]
    public virtual bool CancelWorkPermits { get; set; }

    [XafDisplayName("May cancel visa(s)")]
    public virtual bool CancelVisas { get; set; }

    [XafDisplayName("May cancel border zone permit(s)")]
    public virtual bool CancelBorderZonePermits { get; set; }

    [XafDisplayName("May cancel application(s)")]
    public virtual bool CancelApplicationProfileInstances { get; set; }

    [XafDisplayName("May change invitation(s)")]
    public virtual bool ChangeInvitations { get; set; }

    [XafDisplayName("May change work permit(s)")]
    public virtual bool ChangeWorkPermits { get; set; }

    [XafDisplayName("May change visa(s)")]
    public virtual bool ChangeVisas { get; set; }

    [XafDisplayName("May change border zone permit(s)")]
    public virtual bool ChangeBorderZonePermits { get; set; }

    [XafDisplayName("May change application(s)")]
    public virtual bool ChangeApplicationProfileInstances { get; set; }

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

    public virtual bool RequireRegion { get; set; }
    public virtual Region? DefaultRegion { get; set; }
    public virtual Guid? DefaultRegionId { get; set; }

    public virtual bool RequireCity { get; set; }
    public virtual City? DefaultCity { get; set; }
    public virtual Guid? DefaultCityId { get; set; }

    /// <summary>Legacy From city / To city visibility. Prefer <see cref="RequireRegion"/> and <see cref="RequireCity"/>.</summary>
    public virtual bool RequireRegionCity { get; set; }

    public virtual bool RequireBusinessTripAddress { get; set; }
    public virtual BusinessTripAddress? DefaultBusinessTripAddress { get; set; }
    public virtual Guid? DefaultBusinessTripAddressId { get; set; }

    public virtual bool RequirePurpose { get; set; }

    [MaxLength(700)]
    public virtual string? DefaultPurpose { get; set; }

    public virtual bool RequireProject { get; set; }
    public virtual ProjectContract? DefaultProjectContract { get; set; }
    public virtual Guid? DefaultProjectContractId { get; set; }

    public virtual bool RequireUrgency { get; set; }
    public virtual Urgency? DefaultUrgency { get; set; }
    public virtual Guid? DefaultUrgencyId { get; set; }

    public virtual bool RequireWorkPermitLocation { get; set; }
    [MaxLength(500)]
    public virtual string? DefaultWorkPermitLocation { get; set; }

    /// <summary>
    /// Show Process number on the instance (Case summary / Progress). Forced on when the
    /// template may produce Visa, Work permit, or Invitation; other templates may opt in.
    /// </summary>
    [XafDisplayName("Process number")]
    public virtual bool RequireProcessNumber { get; set; }
    /// <summary>
    /// Preferred shared <see cref="ApprovalLegProfile"/> when creating instances from this via-ministry profile.
    /// Chains themselves are tenant-shared (Configuration); not copied onto the profile.
    /// </summary>
    [XafDisplayName("Default approval legs")]
    public virtual ApprovalLegProfile? DefaultApprovalLegProfile { get; set; }
    public virtual Guid? DefaultApprovalLegProfileId { get; set; }
    public virtual bool RequireEntryDate { get; set; }

    public virtual bool RequireEntryCheckPoint { get; set; }
    public virtual CheckPoint? DefaultEntryCheckPoint { get; set; }
    public virtual Guid? DefaultEntryCheckPointId { get; set; }

    // --- Signatory defaults (per-ApplicationProfileInstance values seeded at create) ---

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
    [InverseProperty(nameof(ApplicationProfileApprovalLegVersion.ApplicationProfile))]
    [XafDisplayName("Approval leg versions")]
    public virtual IList<ApplicationProfileApprovalLegVersion> ApprovalLegVersions { get; set; }

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

    /// <summary>Latest passports to auto-link (1–3). Used only when <see cref="RequirePersonPassport"/> is on. Expiration is not checked.</summary>
    public virtual int PersonPassportLastCount { get; set; } = 1;

    /// <summary>Latest valid visas to auto-link (1–3). Used only when <see cref="RequirePersonVisa"/> is on.</summary>
    public virtual int PersonVisaLastCount { get; set; } = 1;

    /// <summary>Latest valid invitation items to auto-link (1–3). Used only when <see cref="RequirePersonInvitationItem"/> is on.</summary>
    public virtual int PersonInvitationItemLastCount { get; set; } = 1;

    /// <summary>Latest valid work-permit items to auto-link (1–3). Used only when <see cref="RequirePersonWorkPermitItem"/> is on.</summary>
    public virtual int PersonWorkPermitItemLastCount { get; set; } = 1;

    /// <summary>Latest valid border-zone items to auto-link (1–3). Used only when <see cref="RequirePersonBorderZoneItem"/> is on.</summary>
    public virtual int PersonBorderZoneItemLastCount { get; set; } = 1;

    // --- Templates ---

    [Aggregated]
    [InverseProperty(nameof(ApplicationProfileTemplate.ApplicationProfile))]
    [XafDisplayName("Nested templates")]
    public virtual IList<ApplicationProfileTemplate> NestedTemplates { get; set; }

    [Aggregated]
    [InverseProperty(nameof(ApplicationProfileProgressStateSetting.ApplicationProfile))]
    [XafDisplayName("Progress state settings")]
    public virtual IList<ApplicationProfileProgressStateSetting> ProgressStateSettings { get; set; }

    /// <summary>Freeform XAF criteria filtering when this profile appears in pickers.</summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    [XafDisplayName("Applicability criteria")]
    [ToolTip("Optional. When empty, profile is available subject to audience/route rules. Criteria target ApplicationProfileInstance context.")]
    public virtual string? ApplicabilityCriteria { get; set; }

    public virtual bool IsActive { get; set; } = true;

    [InverseProperty(nameof(ApplicationProfileInstance.ApplicationProfile))]
    [VisibleInDetailView(false)]
    public virtual IList<ApplicationProfileInstance> Instances { get; set; }

    [NotMapped]
    [VisibleInDetailView(false)]
    [VisibleInListView(true)]
    public string DisplayName =>
        string.IsNullOrWhiteSpace(SelectionCode)
            ? Name
            : $"{SelectionCode} · {Name}";

    /// <summary>
    /// True when any linked ApplicationProfileInstance has left office preparation / been submitted
    /// (lock state A). Used to make the profile wizard read-only.
    /// </summary>
    [NotMapped]
    [VisibleInListView(true)]
    [XafDisplayName("Config locked")]
    public bool IsConfigLocked =>
        ApplicationProfileLockHelper.IsProfileConfigLocked(this, ObjectSpaceHelper.Get(this));

    public override string ToString() => DisplayName;
}

/// <summary>Exclusive “ApplicationProfileInstance related to” family on <see cref="ApplicationProfile"/>.</summary>
public enum ApplicationProfileActionFamily
{
    Issuance = 0,
    Cancellation = 1,
    Registration = 2,
    BusinessTrip = 3,
    Change = 4
}

/// <summary>
/// Check in, check out, info change, or reg extension when <see cref="ApplicationProfileActionFamily.Registration"/>.
/// <see cref="None"/> for other families.
/// </summary>
public enum ApplicationProfileRegistrationKind
{
    None = 0,
    CheckIn = 1,
    CheckOut = 2,
    InfoChange = 3,
    Extension = 4
}

/// <summary>
/// Named copy of a ministry list on one <see cref="ApplicationProfile"/> (not shared across profiles).
/// Officers pick a version at instance create; the instance stores a ministry snapshot.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(Name))]
public class ApplicationProfileApprovalLegVersion : BaseObject
{
    public ApplicationProfileApprovalLegVersion()
    {
        Legs = new ObservableCollection<ApplicationProfileApprovalLeg>();
    }

    [Browsable(false)]
    public virtual Guid ApplicationProfileId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationProfileId))]
    public virtual ApplicationProfile ApplicationProfile { get; set; } = null!;

    [RuleRequiredField]
    [MaxLength(200)]
    [XafDisplayName("Version name")]
    public virtual string Name { get; set; } = "Version 1";

    [XafDisplayName("Default")]
    public virtual bool IsDefault { get; set; }

    [RuleRequiredField]
    public virtual int? Sequence { get; set; } = 1;

    [Aggregated]
    [InverseProperty(nameof(ApplicationProfileApprovalLeg.ApprovalLegVersion))]
    [XafDisplayName("Legs")]
    public virtual IList<ApplicationProfileApprovalLeg> Legs { get; set; }
}

/// <summary>Ordered ministry approval leg nested on a per-profile <see cref="ApplicationProfileApprovalLegVersion"/>.</summary>
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

    [Browsable(false)]
    public virtual Guid? ApprovalLegVersionId { get; set; }

    [ForeignKey(nameof(ApprovalLegVersionId))]
    public virtual ApplicationProfileApprovalLegVersion? ApprovalLegVersion { get; set; }

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

    /// <summary>Where this nested row came from in the configuration wizard (profile upload vs catalog include).</summary>
    [XafDisplayName("Catalog scope")]
    public virtual ApplicationProfileTemplateCatalogScope CatalogScope { get; set; }
        = ApplicationProfileTemplateCatalogScope.ProfileSpecific;

    /// <summary>Declared merge data family (header / people M2M / both). Extract still discovers tokens.</summary>
    [XafDisplayName("Data scope")]
    public virtual ApplicationProfileTemplateDataScope DataScope { get; set; }
        = ApplicationProfileTemplateDataScope.PeopleM2M;

    /// <summary>When <see cref="CatalogScope"/> is Category — chip key (Invitation, Visa, …).</summary>
    [MaxLength(64)]
    [XafDisplayName("Category key")]
    public virtual string? CategoryKey { get; set; }

    [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
    [XafDisplayName("Template file")]
    public virtual FileData? TemplateFile { get; set; }

    public virtual int SortOrder { get; set; }

    /// <summary>
    /// Profile-specific only. When set, the template is visible on instances whose Project contract matches (Via ministry).
    /// Empty means visible for every instance of this profile.
    /// </summary>
    public virtual ProjectContract? ApplicableProjectContract { get; set; }
    public virtual Guid? ApplicableProjectContractId { get; set; }

    /// <summary>
    /// Profile-specific only. When set, the template is visible on instances whose Migration service matches (Direct migration).
    /// Empty means visible for every instance of this profile.
    /// </summary>
    public virtual MigrationService? ApplicableMigrationService { get; set; }
    public virtual Guid? ApplicableMigrationServiceId { get; set; }

    /// <summary>
    /// When set, this nested template is in the Resminamalar Recycle Bin (hidden from the live catalog).
    /// Catalog Delete sets this; Recycle Bin Restore clears it; Recycle Bin Delete permanently removes the row.
    /// Not XAF deferred deletion (<c>GCRecord</c>).
    /// </summary>
    [Browsable(false)]
    public virtual DateTime? RecycledAtUtc { get; set; }

    [Browsable(false)]
    [MaxLength(255)]
    public virtual string? RecycledByUserName { get; set; }

    [Browsable(false)]
    public virtual DateTime? CreatedOnUtc { get; set; }

    [Browsable(false)]
    [MaxLength(255)]
    public virtual string? CreatedByUserName { get; set; }

    [Browsable(false)]
    public virtual DateTime? ModifiedOnUtc { get; set; }

    [Browsable(false)]
    [MaxLength(255)]
    public virtual string? ModifiedByUserName { get; set; }

    public override void OnSaving()
    {
        base.OnSaving();
        if (ObjectSpace is EFCoreObjectSpace { DbContext: { } dbContext })
        {
            var entry = dbContext.Entry(this);
            if (entry.State == EntityState.Modified)
            {
                var modified = entry.Properties
                    .Where(p => p.IsModified)
                    .Select(p => p.Metadata.Name);
                if (ApplicationProfileLockHelper.IsAllowedResminamalarRecycleBinMutation(
                        isDelete: false,
                        RecycledAtUtc,
                        modified))
                    return;
            }
        }

        TemplateCatalogAuditStamp.Touch(this, SecuritySystem.CurrentUserName);
    }
}

public enum ApplicationProfileTemplateKind
{
    Word = 0,
    Excel = 1,
    PdfForm = 2
}

/// <summary>Wizard catalog bucket for a nested <see cref="ApplicationProfileTemplate"/>.</summary>
public enum ApplicationProfileTemplateCatalogScope
{
    ProfileSpecific = 0,
    Category = 1,
    Global = 2
}

/// <summary>Declared merge data source for a nested template (intent for officers / Extract).</summary>
public enum ApplicationProfileTemplateDataScope
{
    ApplicationHeader = 0,
    PeopleM2M = 1,
    Both = 2
}

/// <summary>Ministry vs migration progress state track on <see cref="ApplicationProfile"/>.</summary>
public enum ApplicationProfileProgressStateTrack
{
    Ministry = 0,
    Migration = 1
}

/// <summary>
/// Per-profile inclusion and SLA tracking for a progress state (legacy wizard checklist).
/// Not used by instance progress. Instance steps follow Directed to, Approval legs, and the fixed transition graph.
/// SLA days live on <see cref="ApplicationProfile.MinistrySlaDays"/> / <see cref="ApplicationProfile.MigrationSlaDays"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationProfileProgressStateSetting : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationProfileId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationProfileId))]
    public virtual ApplicationProfile ApplicationProfile { get; set; } = null!;

    [RuleRequiredField]
    public virtual ApplicationProfileProgressStateTrack? Track { get; set; }

    [RuleRequiredField]
    [MaxLength(64)]
    public virtual string StateCode { get; set; } = string.Empty;

    [XafDisplayName("Include")]
    public virtual bool IsIncluded { get; set; } = true;

    [XafDisplayName("SLA track")]
    public virtual bool IsSlaTracked { get; set; }
}

/// <summary>Progress lock state A helpers for <see cref="ApplicationProfile.IsConfigLocked"/>.</summary>
public static class ApplicationProfileLockHelper
{
    private static readonly string[] UnlockedPrimaryStateCodes =
    [
        "OFFICE_PREPARATION",
        "DRAFT",
        ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
    ];

    public static bool IsPrimaryStateAtOrPastLockStateA(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        return !UnlockedPrimaryStateCodes.Contains(stateCode.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsApplicationAtOrPastLockStateA(ApplicationProfileInstance? application)
    {
        if (application == null)
            return false;

        var code = application.LatestPrimaryStateCode
            ?? application.LatestProgress?.State?.Code;

        return IsPrimaryStateAtOrPastLockStateA(code);
    }

    public static bool IsProfileConfigLocked(ApplicationProfile? profile, IObjectSpace? objectSpace = null)
    {
        if (profile == null)
            return false;

        if (objectSpace != null && !objectSpace.IsNewObject(profile))
        {
            var profileId = profile.ID;
            if (profileId != Guid.Empty)
            {
                // Client-evaluate state codes — IsPrimaryStateAtOrPastLockStateA is not EF-translatable.
                var stateCodes = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                    .Where(a => a.ApplicationProfile != null && a.ApplicationProfile.ID == profileId)
                    .Select(a => a.LatestPrimaryStateCode)
                    .ToList();

                return stateCodes.Any(IsPrimaryStateAtOrPastLockStateA);
            }
        }

        return profile.Instances?.Any(IsApplicationAtOrPastLockStateA) == true;
    }

    public static void EnsureConfigurationEditable(ApplicationProfile profile, IObjectSpace objectSpace)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));

        if (!IsProfileConfigLocked(profile, objectSpace))
            return;

        if (objectSpace.IsNewObject(profile))
            return;

        var original = objectSpace.GetObjectByKey<ApplicationProfile>(profile.ID);
        if (original == null || !HasConfigurationScalarsChanged(original, profile))
            return;

        throw new UserFriendlyException(VisaUiMessages.Get("ApplicationProfile.ConfigLockedCannotEdit"));
    }

    public static void EnsureNestedConfigurationEditable(
        ApplicationProfile? parentProfile,
        IObjectSpace objectSpace,
        object? nested = null)
    {
        if (parentProfile == null || objectSpace == null)
            return;

        if (!IsProfileConfigLocked(parentProfile, objectSpace))
            return;

        if (AllowsNestedEditWhenConfigLocked(nested, objectSpace))
            return;

        throw new UserFriendlyException(VisaUiMessages.Get("ApplicationProfile.ConfigLockedCannotEditNested"));
    }

    /// <summary>
    /// Shared approval-leg chains live on <see cref="ApprovalLegProfile"/> (Configuration).
    /// Per-profile <see cref="ApplicationProfile.DefaultApprovalLegProfile"/> is not a locked scalar
    /// (see <see cref="HasConfigurationScalarsChanged"/>). Legacy nested version rows may still
    /// change while locked because instances keep a snapshot. New nested templates may be added;
    /// Resminamalar Recycle Bin (recycle / restore / purge) may change existing template rows;
    /// other existing template edits stay blocked (see <see cref="EnsureNestedConfigurationEditable"/>).
    /// </summary>
    public static bool AllowsNestedEditWhenConfigLocked(object? nested, IObjectSpace? objectSpace = null) =>
        nested switch
        {
            ApplicationProfileApprovalLegVersion or ApplicationProfileApprovalLeg => true,
            ApplicationProfileTemplate template
                when objectSpace != null && objectSpace.IsNewObject(template) => true,
            ApplicationProfileTemplate template
                when objectSpace != null && IsResminamalarRecycleBinMutation(objectSpace, template) => true,
            _ => false,
        };

    /// <summary>
    /// Recycle Bin only: set/clear <see cref="ApplicationProfileTemplate.RecycledAtUtc"/> or
    /// permanently delete a row that is already in the bin. Does not allow renaming or replacing files.
    /// </summary>
    public static bool IsResminamalarRecycleBinMutation(
        IObjectSpace objectSpace,
        ApplicationProfileTemplate template)
    {
        if (objectSpace == null || template == null)
            return false;

        if (objectSpace.IsObjectToDelete(template))
            return IsAllowedResminamalarRecycleBinMutation(
                isDelete: true,
                template.RecycledAtUtc,
                Array.Empty<string>());

        if (objectSpace is not EFCoreObjectSpace { DbContext: { } dbContext })
            return false;

        var entry = dbContext.Entry(template);
        if (entry.State != EntityState.Modified)
            return false;

        var modified = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => p.Metadata.Name)
            .ToList();

        return IsAllowedResminamalarRecycleBinMutation(
            isDelete: false,
            template.RecycledAtUtc,
            modified);
    }

    internal static bool IsAllowedResminamalarRecycleBinMutation(
        bool isDelete,
        DateTime? recycledAtUtc,
        IEnumerable<string> modifiedMemberNames)
    {
        if (isDelete)
            return recycledAtUtc != null;

        var recycleMembers = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(ApplicationProfileTemplate.RecycledAtUtc),
            nameof(ApplicationProfileTemplate.RecycledByUserName),
            "OptimisticLockField",
            "GCRecord",
        };

        var modified = modifiedMemberNames
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();
        if (modified.Count == 0)
            return false;
        if (modified.Any(n => !recycleMembers.Contains(n)))
            return false;

        return modified.Contains(nameof(ApplicationProfileTemplate.RecycledAtUtc))
            || modified.Contains(nameof(ApplicationProfileTemplate.RecycledByUserName));
    }

    public static bool CanRemoveApprovalLegVersion(
        ApplicationProfile? profile,
        ApplicationProfileApprovalLegVersion? version)
    {
        if (profile == null || version == null)
            return false;

        return ApplicationProfileApprovalLegVersionHelper.GetOrderedVersions(profile)
            .Count(v => !ReferenceEquals(v, version)) >= 1;
    }

    public static void EnsureCanRemoveApprovalLegVersion(
        ApplicationProfile parentProfile,
        ApplicationProfileApprovalLegVersion version,
        IObjectSpace objectSpace)
    {
        if (parentProfile == null || version == null || objectSpace == null)
            return;

        if (!IsProfileConfigLocked(parentProfile, objectSpace))
            return;

        var remaining = ApplicationProfileApprovalLegVersionHelper.GetOrderedVersions(parentProfile)
            .Count(v => !objectSpace.IsObjectToDelete(v));
        if (remaining < 1)
        {
            throw new UserFriendlyException(
                VisaUiMessages.Get("ApplicationProfile.ConfigLockedCannotRemoveLastApprovalLegVersion"));
        }
    }

    public static ApplicationProfile? TryResolveOwningProfile(object? entity, IObjectSpace objectSpace)
    {
        switch (entity)
        {
            case ApplicationProfile profile:
                return profile;
            case ApplicationProfileApprovalLegVersion version:
                return version.ApplicationProfile
                    ?? (version.ApplicationProfileId != Guid.Empty
                        ? objectSpace.GetObjectByKey<ApplicationProfile>(version.ApplicationProfileId)
                        : null);
            case ApplicationProfileApprovalLeg leg:
                return leg.ApplicationProfile
                    ?? leg.ApprovalLegVersion?.ApplicationProfile
                    ?? (leg.ApplicationProfileId != Guid.Empty
                        ? objectSpace.GetObjectByKey<ApplicationProfile>(leg.ApplicationProfileId)
                        : null);
            case ApplicationProfileTemplate template:
                return template.ApplicationProfile
                    ?? (template.ApplicationProfileId != Guid.Empty
                        ? objectSpace.GetObjectByKey<ApplicationProfile>(template.ApplicationProfileId)
                        : null);
            case ApplicationProfileProgressStateSetting stateSetting:
                return stateSetting.ApplicationProfile
                    ?? (stateSetting.ApplicationProfileId != Guid.Empty
                        ? objectSpace.GetObjectByKey<ApplicationProfile>(stateSetting.ApplicationProfileId)
                        : null);
            default:
                return null;
        }
    }

    /// <summary>
    /// Locked-profile scalar compare. Intentionally omits
    /// <see cref="ApplicationProfile.DefaultApprovalLegProfileId"/> so officers can still set
    /// this template's Default while config-locked (shared chains live in Configuration).
    /// </summary>
    internal static bool HasConfigurationScalarsChanged(ApplicationProfile original, ApplicationProfile current) =>
        !string.Equals(original.Name, current.Name, StringComparison.Ordinal)
        || !string.Equals(original.Description, current.Description, StringComparison.Ordinal)
        || !string.Equals(original.Code, current.Code, StringComparison.Ordinal)
        || !string.Equals(original.SelectionCode, current.SelectionCode, StringComparison.Ordinal)
        || original.ProgressRoute != current.ProgressRoute
        || original.ForEmployee != current.ForEmployee
        || original.ForFamilyMember != current.ForFamilyMember
        || original.ForTemporaryVisitor != current.ForTemporaryVisitor
        || original.ActionFamily != current.ActionFamily
        || original.RegistrationKind != current.RegistrationKind
        || original.ProduceInvitation != current.ProduceInvitation
        || original.ProduceWorkPermit != current.ProduceWorkPermit
        || original.ProduceVisa != current.ProduceVisa
        || original.ProduceBorderZone != current.ProduceBorderZone
        || original.ProduceWorkLocation != current.ProduceWorkLocation
        || original.ProduceRejection != current.ProduceRejection
        || original.CancelInvitations != current.CancelInvitations
        || original.CancelWorkPermits != current.CancelWorkPermits
        || original.CancelVisas != current.CancelVisas
        || original.CancelBorderZonePermits != current.CancelBorderZonePermits
        || original.CancelApplicationProfileInstances != current.CancelApplicationProfileInstances
        || original.ChangeInvitations != current.ChangeInvitations
        || original.ChangeWorkPermits != current.ChangeWorkPermits
        || original.ChangeVisas != current.ChangeVisas
        || original.ChangeBorderZonePermits != current.ChangeBorderZonePermits
        || original.ChangeApplicationProfileInstances != current.ChangeApplicationProfileInstances
        || original.RequireVisaType != current.RequireVisaType
        || original.DefaultVisaTypeId != current.DefaultVisaTypeId
        || original.RequireVisaCategory != current.RequireVisaCategory
        || original.DefaultVisaCategoryId != current.DefaultVisaCategoryId
        || original.RequireVisaPeriod != current.RequireVisaPeriod
        || original.DefaultVisaPeriodId != current.DefaultVisaPeriodId
        || original.RequireBorderZone != current.RequireBorderZone
        || !string.Equals(original.DefaultBorderZoneLocation, current.DefaultBorderZoneLocation, StringComparison.Ordinal)
        || original.RequireMigrationService != current.RequireMigrationService
        || original.DefaultMigrationServiceId != current.DefaultMigrationServiceId
        || original.RequireStartDate != current.RequireStartDate
        || original.RequireEndDate != current.RequireEndDate
        || original.RequireRegion != current.RequireRegion
        || original.DefaultRegionId != current.DefaultRegionId
        || original.RequireCity != current.RequireCity
        || original.DefaultCityId != current.DefaultCityId
        || original.RequireRegionCity != current.RequireRegionCity
        || original.RequireBusinessTripAddress != current.RequireBusinessTripAddress
        || original.DefaultBusinessTripAddressId != current.DefaultBusinessTripAddressId
        || original.RequirePurpose != current.RequirePurpose
        || !string.Equals(original.DefaultPurpose, current.DefaultPurpose, StringComparison.Ordinal)
        || original.RequireProject != current.RequireProject
        || original.DefaultProjectContractId != current.DefaultProjectContractId
        || original.RequireUrgency != current.RequireUrgency
        || original.DefaultUrgencyId != current.DefaultUrgencyId
        || original.RequireWorkPermitLocation != current.RequireWorkPermitLocation
        || !string.Equals(original.DefaultWorkPermitLocation, current.DefaultWorkPermitLocation, StringComparison.Ordinal)
        || original.RequireProcessNumber != current.RequireProcessNumber
        || original.RequireEntryDate != current.RequireEntryDate
        || original.RequireEntryCheckPoint != current.RequireEntryCheckPoint
        || original.DefaultEntryCheckPointId != current.DefaultEntryCheckPointId
        || original.DefaultAuthorizedSignatoryId != current.DefaultAuthorizedSignatoryId
        || original.DefaultVisaRepresentativeId != current.DefaultVisaRepresentativeId
        || original.MinistrySlaDays != current.MinistrySlaDays
        || original.MigrationSlaDays != current.MigrationSlaDays
        || original.RequirePersonPassport != current.RequirePersonPassport
        || original.RequirePersonEducation != current.RequirePersonEducation
        || original.RequirePersonPosition != current.RequirePersonPosition
        || original.RequirePersonAddressOfResidence != current.RequirePersonAddressOfResidence
        || original.RequirePersonVisa != current.RequirePersonVisa
        || original.RequirePersonInvitationItem != current.RequirePersonInvitationItem
        || original.RequirePersonWorkPermitItem != current.RequirePersonWorkPermitItem
        || original.RequirePersonBorderZoneItem != current.RequirePersonBorderZoneItem
        || original.RequirePersonSalary != current.RequirePersonSalary
        || original.RequirePersonMedical != current.RequirePersonMedical
        || original.RequirePersonRejectionItem != current.RequirePersonRejectionItem
        || original.RequirePersonTravelHistory != current.RequirePersonTravelHistory
        || original.PersonPassportLastCount != current.PersonPassportLastCount
        || original.PersonVisaLastCount != current.PersonVisaLastCount
        || original.PersonInvitationItemLastCount != current.PersonInvitationItemLastCount
        || original.PersonWorkPermitItemLastCount != current.PersonWorkPermitItemLastCount
        || original.PersonBorderZoneItemLastCount != current.PersonBorderZoneItemLastCount
        || !string.Equals(original.ApplicabilityCriteria, current.ApplicabilityCriteria, StringComparison.Ordinal);
}
