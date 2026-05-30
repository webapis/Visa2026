using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    public abstract class PersonLinkedItemBase<TItem, TParent> : BaseObject, IObjectSpaceLink, ICurrentPersonItem
        where TItem : PersonLinkedItemBase<TItem, TParent>
        where TParent : class, IPersonLinkParent
    {
        [ImmediatePostData]
        [Appearance("PersonLinkedItem_DisableUncheckIsActive", Enabled = false, Criteria = "IsActive")]
        public virtual bool IsActive { get; set; }

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

        public override void OnCreated()
        {
            base.OnCreated();
            CurrentPersonItemSync.OnCreated(this);
        }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}
