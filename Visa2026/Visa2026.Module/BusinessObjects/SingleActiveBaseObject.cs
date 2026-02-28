using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    [Browsable(false)]
    public abstract class SingleActiveBaseObject<TParent, TItem> : BaseObject, IObjectSpaceLink
        where TParent : BaseObject
        where TItem : SingleActiveBaseObject<TParent, TItem>
    {
        private bool isActive;

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
                            }
                        }
                    }
                    SetParentActiveItem(parent, (TItem)this);
                }
                else
                {
                    if (IsParentActiveItem(parent, (TItem)this))
                    {
                        SetParentActiveItem(parent, null);
                    }
                }
            }
        }

        public abstract TParent GetParent();
        public abstract IList<TItem> GetSiblings(TParent parent);
        public abstract void SetParentActiveItem(TParent parent, TItem item);
        public abstract bool IsParentActiveItem(TParent parent, TItem item);

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}