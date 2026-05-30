using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects
{
    [FileAttachment(nameof(File))]
    [RuleCriteria("DocumentFileNotEmpty", DefaultContexts.Save, "File == null or File.Size > 0", "The uploaded file is empty. Attach a non-empty document.")]
    [RuleCriteria("DocumentSizeIsTooLarge", DefaultContexts.Save, "File == null or File.Size <= (MaxDocumentSizeInMB * 1024 * 1024)", "The uploaded document exceeds the maximum allowed size of {MaxDocumentSizeInMB}MB.")]
    public abstract class DocumentBase : BaseObject
    {
        [RuleRequiredField]
        [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
        public virtual FileData File { get; set; }

        [MaxLength(255)]
        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        public virtual string Description { get; set; }

        [NotMapped]
        [Browsable(false)]
        public int MaxDocumentSizeInMB
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                {
                    // Fallback for contexts where ObjectSpace might not be injected.
                    return 5; // 5MB
                }

                return SystemSettings.TryGetInstance(objectSpace)?.MaxDocumentSizeInMB
                       ?? SystemSettings.DefaultMaxDocumentSizeInMB;
            }
        }

        /// <summary>Used by <see cref="RuleFromBoolPropertyAttribute"/> to enforce <see cref="DocumentFileUploadConstraints"/>.</summary>
        [NotMapped]
        [Browsable(false)]
        [RuleFromBoolProperty("DocumentBase_FileContentValid", DefaultContexts.Save,
            "The attachment must use an allowed type (PDF, PNG, JPEG, TIFF, GIF, or BMP) and the file content must match the extension (renamed or corrupt files are rejected).")]
        public bool IsDocumentFileContentValid => DocumentFileUploadConstraints.TryValidate(File, out _);
    }
}