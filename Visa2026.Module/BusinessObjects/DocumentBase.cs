using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [FileAttachment(nameof(File))]
    [RuleCriteria("DocumentSizeIsTooLarge", DefaultContexts.Save, "File == null or File.Size <= (MaxDocumentSizeInMB * 1024 * 1024)", "The uploaded document exceeds the maximum allowed size of {MaxDocumentSizeInMB}MB.")]
    public abstract class DocumentBase : BaseObject, IObjectSpaceLink
    {
        [RuleRequiredField]
        [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
        public virtual FileData File { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }

        [NotMapped]
        [Browsable(false)]
        public int MaxDocumentSizeInMB
        {
            get
            {
                if (ObjectSpace == null)
                {
                    // Fallback for contexts where ObjectSpace might not be injected.
                    return 5; // 5MB
                }

                return SystemSettings.TryGetInstance(ObjectSpace)?.MaxDocumentSizeInMB
                       ?? SystemSettings.DefaultMaxDocumentSizeInMB;
            }
        }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}