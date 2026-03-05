using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    public abstract class ImageBase : BaseObject
    {
        [RuleRequiredField]
        [ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
        public virtual byte[] Image { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }

        [Browsable(false)]
        [RuleFromBoolProperty("ImageSizeIsTooLarge", DefaultContexts.Save, "The uploaded image exceeds the maximum allowed size of 2MB.")]
        public bool IsImageSizeValid
        {
            get
            {
                // The [RuleRequiredField] on the Image property handles the null case.
                // This rule checks the size if an image is provided.
                const int maxFileSizeInBytes = 2 * 1024 * 1024; // 2MB
                if (Image != null && Image.Length > maxFileSizeInBytes)
                {
                    return false;
                }
                return true;
            }
        }
    }
}