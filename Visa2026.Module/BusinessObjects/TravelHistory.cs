using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem(false)]
    [DefaultProperty(nameof(Title))]
    [Appearance("ReadOnlyFixedFieldsInSubclasses", Criteria = "IsFixedMovement", 
        TargetItems = "TravelType;MovementType", Enabled = false)]
    public abstract class TravelHistory : SingleActiveBaseObject<Person, TravelHistory>, ISoftDelete
    {
        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime TravelDate { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("AllowEdit", "False")]
        public virtual TravelType? TravelType { get; set; }

        [RuleRequiredField]
        [ModelDefault("AllowEdit", "False")]
        public virtual MovementType? MovementType { get; set; }

        [Appearance("CheckPointVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "TravelType != 'External'", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "TravelType = 'External'")]
        public virtual CheckPoint CheckPoint { get; set; }

        public virtual PurposeOfTravel PurposeOfTravel { get; set; }

        public virtual string Notes { get; set; }

        [NotMapped]
        public string Title => $"{Person?.FullName} - {MovementType} on {TravelDate:d}";

        [Browsable(false)]
        [NotMapped]
        public virtual bool IsFixedMovement => false;

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.TravelDate;

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<TravelHistory> GetSiblings(Person parent)
        {
            return parent?.TravelHistories;
        }

        public override void SetParentActiveItem(Person parent, TravelHistory item)
        {
            parent.CurrentTravelHistory = item;
        }

        public override bool IsParentActiveItem(Person parent, TravelHistory item)
        {
            return parent.CurrentTravelHistory == item;
        }

        public override void OnCreated()
        {
            base.OnCreated();
            TravelDate = DateTime.Today;
            if (ObjectSpace != null)
            {
                CheckPoint = ObjectSpace.GetObjectsQuery<CheckPoint>().FirstOrDefault(x => x.IsDefault);
                PurposeOfTravel = ObjectSpace.GetObjectsQuery<PurposeOfTravel>().FirstOrDefault(x => x.IsDefault);
            }
        }
    }

    [XafDisplayName("External Arrival")]
    public class ExternalArrival : TravelHistory
    {
        public override bool IsFixedMovement => true;

        public override void OnCreated()
        {
            base.OnCreated();
            TravelType = BusinessObjects.TravelType.External;
            MovementType = BusinessObjects.MovementType.Entry;
        }
    }

    [XafDisplayName("External Departure")]
    public class ExternalDeparture : TravelHistory
    {
        public override bool IsFixedMovement => true;

        public override void OnCreated()
        {
            base.OnCreated();
            TravelType = BusinessObjects.TravelType.External;
            MovementType = BusinessObjects.MovementType.Exit;
        }
    }

    [XafDisplayName("Internal Arrival")]
    public class InternalArrival : TravelHistory
    {
        public override bool IsFixedMovement => true;

        public override void OnCreated()
        {
            base.OnCreated();
            TravelType = BusinessObjects.TravelType.Internal;
            MovementType = BusinessObjects.MovementType.Entry;
        }
    }

    [XafDisplayName("Internal Departure")]
    public class InternalDeparture : TravelHistory
    {
        public override bool IsFixedMovement => true;

        public override void OnCreated()
        {
            base.OnCreated();
            TravelType = BusinessObjects.TravelType.Internal;
            MovementType = BusinessObjects.MovementType.Exit;
        }
    }
}
