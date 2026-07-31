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
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Invitation letter header (legacy ApplicationResult Result=Invitation).
    /// No Netije/Result property — Rejection is a separate BO.
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("Invitation")]
    [DefaultProperty(nameof(InvitationNumber))]
    [RuleCriteria("Invitation_ExpirationAfterIssued", DefaultContexts.Save,
        "ExpirationDate > IssuedDate",
        "Expiration Date must be later than Issued Date.")]
    [RuleCriteria("Invitation_VisaWindowOrder", DefaultContexts.Save,
        "Not IsVisaStartAndEndDateDefined Or VisaEndDate > VisaStartDate",
        "Visa End Date must be later than Visa Start Date when the visa window is defined.")]
    [Appearance("Invitation_VisaWindowHidden", Visibility = ViewItemVisibility.Hide,
        Criteria = "!IsVisaStartAndEndDateDefined",
        TargetItems = "VisaStartDate;VisaEndDate",
        Context = "DetailView")]
    [SupportsOptionalDetailFields]
    public class Invitation : BaseObject, IExpirationLogic, IPersonLinkParent, IOptionalDetailFields
    {
        public Invitation()
        {
            InvitationItems = new ObservableCollection<InvitationItem>();
            Images = new ObservableCollection<InvitationImage>();
            Documents = new ObservableCollection<InvitationDocument>();
        }

        [MaxLength(50)]
        [RuleRequiredField]
        [XafDisplayName("Number")]
        public virtual string InvitationNumber { get; set; }

        /// <summary>
        /// Formalization / issued date (legacy Resmileşdirilen sene).
        /// Stored in column <c>StartDate</c> for schema compatibility with existing databases.
        /// </summary>
        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [XafDisplayName("Issued Date")]
        [Column("StartDate")]
        public virtual DateTime IssuedDate { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [XafDisplayName("Visa Category")]
        public virtual VisaCategory VisaCategory { get; set; }

        /// <summary>Intended visa period on the invitation (legacy Wiza möhleti). Not used to compute invitation expiry.</summary>
        [RuleRequiredField]
        [ImmediatePostData]
        [XafDisplayName("Visa Period")]
        public virtual VisaPeriod VisaPeriod { get; set; }

        /// <summary>When true, <see cref="VisaStartDate"/> and <see cref="VisaEndDate"/> apply (legacy Visa Start And End Date Defined).</summary>
        [ImmediatePostData]
        [VisibleInListView(false)]
        [XafDisplayName("Visa Start And End Date Defined")]
        public virtual bool IsVisaStartAndEndDateDefined { get; set; }

        [RuleRequiredField(TargetCriteria = "IsVisaStartAndEndDateDefined")]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [VisibleInListView(false)]
        [XafDisplayName("Visa Start Date")]
        public virtual DateTime? VisaStartDate { get; set; }

        [RuleRequiredField(TargetCriteria = "IsVisaStartAndEndDateDefined")]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [VisibleInListView(false)]
        [XafDisplayName("Visa End Date")]
        public virtual DateTime? VisaEndDate { get; set; }

        /// <summary>Invitation letter expiry (legacy Möhleti tamamlanýan sene / DateOfExpire).</summary>
        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [XafDisplayName("Expiration Date")]
        public virtual DateTime? ExpirationDate { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        [XafDisplayName(" ")]
        public bool ShowOptionalFields { get; set; }

        /// <summary>
        /// Optional link to a visa application. When set, invitation items are limited to people on that application.
        /// Only applications whose type has <see cref="ApplicationType.CanIssueInvitation"/> are offered.
        /// </summary>
        [ImmediatePostData]
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        [VisibleInLookupListView(false)]
        [DataSourceProperty(nameof(AvailableApplications))]
        [ToolTip("Link this invitation to an application when one exists. Leave empty for standalone invitations.")]
        public virtual Application Application { get; set; }

        /// <summary>
        /// Candidate applications for <see cref="Application"/> (types that may produce an invitation).
        /// </summary>
        [NotMapped]
        [Browsable(false)]
        public IList<Application> AvailableApplications
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                    return new List<Application>();

                return objectSpace.GetObjectsQuery<Application>()
                    .Where(a => a.ApplicationType != null && a.ApplicationType.CanIssueInvitation)
                    .OrderByDescending(a => a.ApplicationDate)
                    .ThenBy(a => a.FullApplicationNumber)
                    .ToList();
            }
        }

        [RuleFromBoolProperty(
            "Invitation_ApplicationTypeAllowed",
            DefaultContexts.Save,
            "Application must be a type that can issue an invitation.")]
        [Browsable(false)]
        public bool IsApplicationTypeAllowed
        {
            get
            {
                if (Application == null)
                    return true;
                return ApplicationTypeCapabilities.CanIssueInvitation(Application.ApplicationType);
            }
        }

        [Aggregated]
        [InverseProperty(nameof(InvitationItem.Invitation))]
        public virtual IList<InvitationItem> InvitationItems { get; set; }

        [XafDisplayName("Person count")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [NotMapped]
        public int TotalPersonCount => listViewTotalPersonCount ?? InvitationItems?.Count ?? 0;

        private int? listViewTotalPersonCount;

        public void SetListViewTotalPersonCount(int count) => listViewTotalPersonCount = count;

        [Aggregated]
        [InverseProperty(nameof(InvitationImage.Invitation))]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<InvitationImage> Images { get; set; }

        [Aggregated]
        [InverseProperty(nameof(InvitationDocument.Invitation))]
        public virtual IList<InvitationDocument> Documents { get; set; }

        [ModelDefault("AllowEdit", "False")]
        [XafDisplayName("Days Remaining")]
        public int DaysRemaining => ExpirationLogicHelper.CalculateDaysRemaining(ExpirationDate);

        [Browsable(false)]
        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(
                    this,
                    ExpirationAlertBusinessObjectKeys.Invitation,
                    ObjectSpaceHelper.Get(this));
            }
        }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Person> AvailablePeople
        {
            get
            {
                if (Application?.ApplicationItems != null)
                {
                    return Application.ApplicationItems
                        .Select(ai => ai.Person)
                        .Where(p => p != null)
                        .ToList()!;
                }

                IObjectSpace? objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                {
                    return Array.Empty<Person>();
                }

                return objectSpace.GetObjectsQuery<Person>()
                    .Where(p => !p.IsArchived)
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToList();
            }
        }

        public override void OnCreated()
        {
            base.OnCreated();
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null)
            {
                VisaCategory ??= objectSpace.GetObjectsQuery<VisaCategory>().FirstOrDefault(v => v.IsDefault);
                VisaPeriod ??= objectSpace.GetObjectsQuery<VisaPeriod>().FirstOrDefault(v => v.IsDefault);
            }

            if (IssuedDate == default)
            {
                IssuedDate = DateTime.Today;
            }
        }

        /// <summary>ListView link column that opens header document copies in the preview slot.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public string DocumentCopiesListLink =>
            Visa2026.Module.Localization.VisaUiMessages.Get("InvitationDocumentCopies.List.ColumnLink");
    }
}