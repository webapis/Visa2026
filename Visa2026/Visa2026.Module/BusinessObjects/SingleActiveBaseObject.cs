using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    public abstract class SingleActiveBaseObject<TParent, TItem> : BaseObject, IObjectSpaceLink
        where TParent : BaseObject
        where TItem : SingleActiveBaseObject<TParent, TItem>
    {
        private bool isActive;

        [ImmediatePostData]
        [Appearance("SingleActiveBaseObject_DisableUncheck", Enabled = false, Criteria = "IsActive")]
        public virtual bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        public override void OnCreated()
        {
            base.OnCreated();
            IsActive = true;
        }

        public override void OnSaving()
        {
            base.OnSaving();
            UpdateActiveState();
        }

        protected virtual void UpdateActiveState()
        {
            TParent parent = GetParent();
            if (parent != null)
            {
                if (IsActive)
                {
                    IList<TItem> siblings = GetSiblings(parent);
                    if (siblings != null)
                    {
                        foreach (TItem sibling in siblings.ToList())
                        {
                            if (sibling != this && sibling.IsActive)
                            {
                                sibling.IsActive = false;
                                ClearAdditionalActiveItems(sibling);
                            }
                        }
                    }
                    SetParentActiveItem(parent, (TItem)this);
                    SetAdditionalActiveItems((TItem)this);
                }
                else
                {
                    if (IsParentActiveItem(parent, (TItem)this))
                    {
                        SetParentActiveItem(parent, null);
                        ClearAdditionalActiveItems((TItem)this);
                    }
                }
            }
        }

        public abstract TParent GetParent();
        public abstract IList<TItem> GetSiblings(TParent parent);
        public abstract void SetParentActiveItem(TParent parent, TItem item);
        public abstract bool IsParentActiveItem(TParent parent, TItem item);

        /// <summary>
        /// A hook for derived classes to set additional properties on related objects when this item becomes active.
        /// For example, setting Passport.CurrentVisa when a Visa is activated.
        /// </summary>
        /// <param name="item">The item being activated (this).</param>
        protected virtual void SetAdditionalActiveItems(TItem item) { }

        /// <summary>
        /// A hook for derived classes to clear properties on related objects when an item is deactivated.
        /// </summary>
        /// <param name="item">The item being deactivated.</param>
        protected virtual void ClearAdditionalActiveItems(TItem item) { }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}