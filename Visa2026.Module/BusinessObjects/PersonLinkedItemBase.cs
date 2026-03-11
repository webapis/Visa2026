using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    public abstract class PersonLinkedItemBase<TItem, TParent> : SingleActiveBaseObject<Person, TItem>
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

        public override Person GetParent()
        {
            return Person;
        }
    }
}