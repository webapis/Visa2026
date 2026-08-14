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
using Visa2026.Module.Documentation;

namespace Visa2026.Module.BusinessObjects
{
    [UserDocumentation("applications/progress", Category = "ApplicationProfileInstances")]
    [Table("ApplicationProfileInstanceProgresses")]
[DefaultClassOptions]
    [DefaultProperty(nameof(State))]
    [FileAttachment(nameof(MinistryLetterFile))]
    [RuleCriteria(
        "ApplicationProfileInstanceProgress_MinistryLetterFileNotEmpty",
        DefaultContexts.Save,
        "MinistryLetterFile == null or MinistryLetterFile.Size > 0",
        "The uploaded ministry letter copy is empty.")]
    [RuleCriteria(
        "ApplicationProfileInstanceProgress_MinistryLetterFileSize",
        DefaultContexts.Save,
        "MinistryLetterFile == null or MinistryLetterFile.Size <= (MaxDocumentSizeInMB * 1024 * 1024)",
        "The ministry letter copy exceeds the maximum allowed size of {MaxDocumentSizeInMB}MB.")]
    public class ApplicationProfileInstanceProgress : BaseObject
    {
        [RuleRequiredField]
        public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [DataSourceProperty(nameof(AvailableStatesForNextStep))]
        public virtual ApplicationState State { get; set; }

        [Browsable(false)]
        [NotMapped]
        public IList<ApplicationState> AvailableStatesForNextStep => LoadAvailableStatesForNextStep();

        /// <summary>1-based step sequence within the parent application's progress history.</summary>
        [Column("ProgressOrder")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        public virtual int Order { get; set; }

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime Date { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }

        /// <summary>
        /// Migration-service processing number (legacy Işlenmäge başlanan belgi).
        /// Canonical on <c>PROCESS_STARTED</c>; may also appear on direct-migration <c>PROCESS_ISSUED</c>.
        /// </summary>
        [XafDisplayName("Process number")]
        [MaxLength(100)]
        public virtual string? ProcessNumber { get; set; }

        [XafDisplayName("Ministrlik")]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [NotMapped]
        public string MinistryStepLabel =>
            ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep(
                ApplicationProfileInstance,
                State?.Code,
                locationCode: null) ?? string.Empty;

        /// <summary>Progress history list: localized state; appends ministry short name at ministry legs.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public string StatusListLabel =>
            ApplicationProfileInstanceProgressListLabelHelper.FormatStatusLabel(
                State?.ToString(),
                MinistryStepLabel);

        [Browsable(false)]
        [NotMapped]
        public bool IsMinistryDecisionStep =>
            ApplicationProfileInstanceProgressLegCodes.IsMinistryDecisionStateCode(State?.Code);

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
            "ApplicationProfileInstanceProgress_MinistryLetterFileContentValid",
            DefaultContexts.Save,
            "The ministry letter copy must use an allowed type (PDF, PNG, JPEG, TIFF, GIF, or BMP) and the file content must match the extension.")]
        public bool IsMinistryLetterFileContentValid =>
            MinistryLetterFile == null || DocumentFileUploadConstraints.TryValidate(MinistryLetterFile, out _);

        public override void OnCreated()
        {
            base.OnCreated();
            Date = DateTime.Now;
            ApplicationProfileInstanceProgressTransitionHelper.TryApplySuggestedNextStep(this);
            TryAssignOrder();
        }

        public override void OnSaving()
        {
            TryAssignOrder();
            if (ApplicationProfileInstance != null)
            {
                ApprovalLegProfileMinistryHelper.EnsureSnapshots(
                    ObjectSpaceHelper.Get(this) ?? ObjectSpaceHelper.Get(ApplicationProfileInstance),
                    ApplicationProfileInstance);
            }

            base.OnSaving();
            if (ApplicationProfileInstance != null)
                ApplicationLatestProgressSyncHelper.Sync(ApplicationProfileInstance, ObjectSpaceHelper.Get(this));
        }

        public virtual void OnDeleting()
        {
            var parent = ApplicationProfileInstance;
            if (parent != null)
                ApplicationLatestProgressSyncHelper.Sync(parent, ObjectSpaceHelper.Get(this));
        }

        private void TryAssignOrder()
        {
            if (Order > 0 || ApplicationProfileInstance == null)
                return;

            var objectSpace = ObjectSpaceHelper.Get(this) ?? ObjectSpaceHelper.Get(ApplicationProfileInstance);
            if (objectSpace == null)
                return;

            Order = ApplicationProfileInstanceProgressOrderHelper.ResolveNextOrder(this, objectSpace);
        }

        private IList<ApplicationState> LoadAvailableStatesForNextStep()
        {
            var objectSpace = ObjectSpaceHelper.Get(this) ?? ObjectSpaceHelper.Get(ApplicationProfileInstance);
            if (objectSpace == null || ApplicationProfileInstance == null)
                return Array.Empty<ApplicationState>();

            var allowedCodes = ApplicationProfileInstanceProgressTransitionHelper
                .GetAllowedStateCodesForProgressRow(this, objectSpace)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return objectSpace.GetObjectsQuery<ApplicationState>()
                .Where(s => s.Code != null && allowedCodes.Contains(s.Code))
                .OrderBy(s => s.Code)
                .ToList();
        }
    }
}