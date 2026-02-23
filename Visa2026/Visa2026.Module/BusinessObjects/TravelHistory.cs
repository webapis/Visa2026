using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    [DefaultProperty(nameof(Title))]
    public class TravelHistory : SingleActiveBaseObject<Person, TravelHistory>
    {
        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        public virtual DateTime TravelDate { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        public virtual TravelType? TravelType { get; set; }

        [RuleRequiredField]
        public virtual MovementType? MovementType { get; set; }

        [Appearance("CheckPointVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "TravelType != 'External'", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "TravelType = 'External'")]
        public virtual CheckPoint CheckPoint { get; set; }

        [MaxLength(100)]
        public virtual string FromLocation { get; set; }

        [MaxLength(100)]
        public virtual string ToLocation { get; set; }

        public virtual PurposeOfTravel PurposeOfTravel { get; set; }

        public virtual string Notes { get; set; }

        [NotMapped]
        public string Title => $"{Person?.FullName} - {MovementType} on {TravelDate:d}";

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
    }
}