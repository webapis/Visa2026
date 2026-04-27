using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [RuleCriteria("ImageSizeIsTooLarge", DefaultContexts.Save, "Image == null or Image.Length <= (MaxImageSizeInMB * 1024 * 1024)", "The uploaded image exceeds the maximum allowed size of {MaxImageSizeInMB}MB.")]
    public abstract class ImageBase : BaseObject, IObjectSpaceLink
    {
        [RuleRequiredField]
        [ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
        public virtual byte[] Image { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }

        [Browsable(false)]
        [NotMapped]
        public int MaxImageSizeInMB
        {
            get
            {
                if (ObjectSpace == null)
                {
                    // Fallback for contexts where ObjectSpace might not be injected, like unit tests.
                    return 2; // 2MB
                }

                return SystemSettings.TryGetInstance(ObjectSpace)?.MaxImageSizeInMB
                       ?? SystemSettings.DefaultMaxImageSizeInMB;
            }
        }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}