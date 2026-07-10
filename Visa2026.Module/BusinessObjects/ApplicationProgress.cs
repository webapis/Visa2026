using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(State))]
    [FileAttachment(nameof(MinistryLetterFile))]
    [RuleCriteria(
        "ApplicationProgress_MinistryLetterFileNotEmpty",
        DefaultContexts.Save,
        "MinistryLetterFile == null or MinistryLetterFile.Size > 0",
        "The uploaded ministry letter copy is empty.")]
    [RuleCriteria(
        "ApplicationProgress_MinistryLetterFileSize",
        DefaultContexts.Save,
        "MinistryLetterFile == null or MinistryLetterFile.Size <= (MaxDocumentSizeInMB * 1024 * 1024)",
        "The ministry letter copy exceeds the maximum allowed size of {MaxDocumentSizeInMB}MB.")]
    //[RuleCriteria("ApplicationProgressDateNotInFuture", DefaultContexts.Save, "Date <= Now()", "Date cannot be in the future.")]
    //[RuleCriteria("ApplicationProgressDateNotBeforeApplicationDate", DefaultContexts.Save, "Date >= Application.ApplicationDate", "Progress date cannot be earlier than the application date.")]
    public class ApplicationProgress : BaseObject
    {
        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [DataSourceProperty(nameof(AvailableStatesForNextStep))]
        public virtual ApplicationState State { get; set; }

        [RuleRequiredField]
        [DataSourceProperty(nameof(AvailableLocationsForSelectedState))]
        public virtual ApplicationLocation Location { get; set; }

        [Browsable(false)]
        [NotMapped]
        public IList<ApplicationState> AvailableStatesForNextStep => LoadAvailableStatesForNextStep();

        [Browsable(false)]
        [NotMapped]
        public IList<ApplicationLocation> AvailableLocationsForSelectedState => LoadAvailableLocationsForSelectedState();

        /// <summary>1-based step sequence within the parent application's progress history.</summary>
        [Column("ProgressOrder")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(true)]
        public virtual int Order { get; set; }

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime Date { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }

        [XafDisplayName("Ministrlik")]
        [VisibleInListView(false)]
        [NotMapped]
        public string MinistryStepLabel =>
            ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep(
                Application,
                State?.Code,
                Location?.Code) ?? string.Empty;

        /// <summary>Progress history list: localized location plus ministry short name when at a ministry leg.</summary>
        [NotMapped]
        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        public string LocationWithMinistryLabel
        {
            get
            {
                var locationLabel = Location?.ToString() ?? string.Empty;
                var ministry = MinistryStepLabel;
                if (string.IsNullOrWhiteSpace(ministry))
                    return locationLabel;
                if (string.IsNullOrWhiteSpace(locationLabel))
                    return ministry;
                return $"{locationLabel} - {ministry}";
            }
        }

        /// <summary>Progress history list: localized state; appends ministry short name at ministry legs (no location prefix).</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public string StatusListLabel =>
            ApplicationProgressListLabelHelper.FormatStatusLabel(
                State?.ToString(),
                Location?.Code,
                MinistryStepLabel);

        [Browsable(false)]
        [NotMapped]
        public bool IsMinistryDecisionStep =>
            ApplicationProgressLegCodes.IsMinistryDecisionStateCode(State?.Code);

        [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public virtual FileData MinistryLetterFile { get; set; }

        [NotMapped]
        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        public string MinistryLetterFileName => MinistryLetterFile?.FileName ?? string.Empty;

        [NotMapped]
        [Browsable(false)]
        public int MaxDocumentSizeInMB
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                return objectSpace == null
                    ? SystemSettings.DefaultMaxDocumentSizeInMB
                    : SystemSettings.TryGetInstance(objectSpace)?.MaxDocumentSizeInMB
                      ?? SystemSettings.DefaultMaxDocumentSizeInMB;
            }
        }

        [NotMapped]
        [Browsable(false)]
        [RuleFromBoolProperty(
            "ApplicationProgress_MinistryLetterFileContentValid",
            DefaultContexts.Save,
            "The ministry letter copy must use an allowed type (PDF, PNG, JPEG, TIFF, GIF, or BMP) and the file content must match the extension.")]
        public bool IsMinistryLetterFileContentValid =>
            MinistryLetterFile == null || DocumentFileUploadConstraints.TryValidate(MinistryLetterFile, out _);

        public override void OnCreated()
        {
            base.OnCreated();
            Date = DateTime.Now;
            ApplicationProgressTransitionHelper.TryApplySuggestedNextStep(this);
            TryAssignOrder();
        }

        public override void OnSaving()
        {
            TryAssignOrder();
            if (Application != null)
            {
                ApprovalLegProfileMinistryHelper.EnsureSnapshots(
                    ObjectSpaceHelper.Get(this) ?? ObjectSpaceHelper.Get(Application),
                    Application);
            }

            base.OnSaving();
            if (Application != null)
                ApplicationLatestProgressSyncHelper.Sync(Application, ObjectSpaceHelper.Get(this));
        }

        public virtual void OnDeleting()
        {
            var parent = Application;
            if (parent != null)
                ApplicationLatestProgressSyncHelper.Sync(parent, ObjectSpaceHelper.Get(this));
        }

        private void TryAssignOrder()
        {
            if (Order > 0 || Application == null)
                return;

            var objectSpace = ObjectSpaceHelper.Get(this) ?? ObjectSpaceHelper.Get(Application);
            if (objectSpace == null)
                return;

            Order = ApplicationProgressOrderHelper.ResolveNextOrder(this, objectSpace);
        }

        private IList<ApplicationState> LoadAvailableStatesForNextStep()
        {
            var objectSpace = ObjectSpaceHelper.Get(this) ?? ObjectSpaceHelper.Get(Application);
            if (objectSpace == null || Application == null)
                return Array.Empty<ApplicationState>();

            var allowedCodes = ApplicationProgressTransitionHelper
                .GetAllowedStateCodesForProgressRow(this, objectSpace)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return objectSpace.GetObjectsQuery<ApplicationState>()
                .Where(s => s.Code != null && allowedCodes.Contains(s.Code))
                .OrderBy(s => s.Code)
                .ToList();
        }

        private IList<ApplicationLocation> LoadAvailableLocationsForSelectedState()
        {
            var objectSpace = ObjectSpaceHelper.Get(this) ?? ObjectSpaceHelper.Get(Application);
            if (objectSpace == null || Application == null)
                return Array.Empty<ApplicationLocation>();

            var allowedCodes = ApplicationProgressTransitionHelper
                .GetAllowedLocationCodesForProgressRow(this, objectSpace)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return objectSpace.GetObjectsQuery<ApplicationLocation>()
                .Where(l => l.Code != null && allowedCodes.Contains(l.Code))
                .OrderBy(l => l.Code)
                .ToList();
        }
    }
}
