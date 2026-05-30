using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    public abstract class PersonLinkedItemBase<TItem, TParent> : BaseObject
        where TItem : PersonLinkedItemBase<TItem, TParent>
        where TParent : class, IPersonLinkParent
    {
        [Browsable(false)]
        public abstract TParent ParentObject { get; }

        [RuleRequiredField]
        [DataSourceProperty("ParentObject.AvailablePeople")]
        public virtual Person Person { get; set; }

        [Browsable(false)]
        public virtual bool IsPersonValid
        {
            get
            {
                if (Person == null || ParentObject?.Application == null) return true;
                return ParentObject.Application.ApplicationItems.Any(ai => ai.Person?.ID == Person.ID);
            }
        }
    }
}
