using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    public abstract class SingleActiveBaseObject<TParent, TItem> : BaseObject
        where TParent : BaseObject
        where TItem : SingleActiveBaseObject<TParent, TItem>
    {
        private bool isActive;

        public virtual bool IsActive
        {
            get => isActive;
            set
            {
                if (isActive != value)
                {
                    isActive = value;
                    UpdateActiveState();
                }
            }
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
    }
}