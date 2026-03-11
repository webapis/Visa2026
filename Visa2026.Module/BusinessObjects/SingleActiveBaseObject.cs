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

            // If this object supports soft-deletion...
            if (this is ISoftDelete softDeletableObject)
            {
                // ...and it has been marked as deleted, then it can no longer be considered active.
                // Setting IsActive to false here will trigger the deactivation logic in UpdateActiveState.
                if (softDeletableObject.IsDeleted && this.IsActive)
                {
                    this.IsActive = false;
                }
            }
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
                        // Create a working list of siblings to consider for deactivation.
                        IEnumerable<TItem> siblingsToConsider = siblings.ToList();

                        // If the items support soft deletion, filter out the deleted ones.
                        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TItem)))
                        {
                            siblingsToConsider = siblingsToConsider.Where(s => !((ISoftDelete)s).IsDeleted);
                        }

                        // Now, iterate through the filtered list.
                        foreach (TItem sibling in siblingsToConsider)
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

                        // Attempt to find the next most recent, non-deleted item to activate.
                        var nextActive = GetSiblings(parent)?
                            .Where(s => s.ID != this.ID) // Exclude the item being deactivated
                            .Where(s =>
                            {
                                // Exclude soft-deleted items if applicable
                                if (s is ISoftDelete sd) return !sd.IsDeleted;
                                return true;
                            })
                            .OrderByDescending(s => s.ChronologicalSortDate) // Order by the new virtual property
                            .FirstOrDefault(s => s.ChronologicalSortDate.HasValue); // Only consider items that provide a date

                        if (nextActive != null)
                        {
                            // Activate the found item. This will trigger its own UpdateActiveState,
                            // which will correctly set the parent's current item.
                            nextActive.IsActive = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A hook for derived classes to provide a date for chronological sorting.
        /// Override this property to enable automatic activation of the next most recent item upon deactivation.
        /// </summary>
        [Browsable(false)]
        [NotMapped]
        protected virtual DateTime? ChronologicalSortDate => null;

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