using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [RuleCriteria("ImageNotEmpty", DefaultContexts.Save, "Image == null or Image.Length > 0", "The uploaded image is empty. Choose a non-empty image.")]
    [RuleCriteria("ImageSizeIsTooLarge", DefaultContexts.Save, "Image == null or Image.Length <= (MaxImageSizeInMB * 1024 * 1024)", "The uploaded image exceeds the maximum allowed size of {MaxImageSizeInMB}MB.")]
    public abstract class ImageBase : BaseObject
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
                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                {
                    // Fallback for contexts where ObjectSpace might not be injected, like unit tests.
                    return 2; // 2MB
                }

                return SystemSettings.TryGetInstance(objectSpace)?.MaxImageSizeInMB
                       ?? SystemSettings.DefaultMaxImageSizeInMB;
            }
        }
    }
}