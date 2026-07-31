using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem(false)]
    [DefaultProperty(nameof(Title))]
    public abstract class TravelHistory : BaseObject
    {
        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime TravelDate { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        public virtual TravelType? TravelType
        {
            get => travelType;
            set
            {
                if (travelType == value)
                    return;

                travelType = value;
                if (value == BusinessObjects.TravelType.Internal)
                {
                    CheckPoint = null;
                    Country = null;
                }
                else if (value == BusinessObjects.TravelType.External)
                {
                    Region = null;
                    City = null;
                }
            }
        }
        private TravelType? travelType;

        [RuleRequiredField]
        [ImmediatePostData]
        public virtual MovementType? MovementType { get; set; }

        [Appearance("CheckPointVisible", Visibility = ViewItemVisibility.Hide, Criteria = "TravelType != 'External'", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "TravelType = 'External'")]
        public virtual CheckPoint CheckPoint { get; set; }

        [Appearance("TravelCountryVisible", Visibility = ViewItemVisibility.Hide, Criteria = "TravelType != 'External'", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "TravelType = 'External'")]
        public virtual Country Country { get; set; }

        [Appearance("TravelRegionVisible", Visibility = ViewItemVisibility.Hide, Criteria = "TravelType != 'Internal'", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "TravelType = 'Internal'")]
        [ImmediatePostData]
        public virtual Region Region
        {
            get => region;
            set
            {
                if (region == value)
                    return;

                region = value;
                City = null;
            }
        }
        private Region region;

        [Appearance("TravelCityVisible", Visibility = ViewItemVisibility.Hide, Criteria = "TravelType != 'Internal'", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "TravelType = 'Internal'")]
        [DataSourceCriteria("[Region] = '@This.Region'")]
        public virtual City City { get; set; }

        [XafDisplayName("Travel Notes")]
        public virtual string Notes { get; set; }

        [NotMapped]
        public string Title => $"{Person?.FullName} - {MovementType} on {TravelDate:d}";


        public override void OnCreated()
        {
            base.OnCreated();
            TravelDate = DateTime.Today;
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null)
            {
                CheckPoint = objectSpace.GetObjectsQuery<CheckPoint>().FirstOrDefault(x => x.IsDefault);
                Country = objectSpace.GetObjectsQuery<Country>().FirstOrDefault(c => c.IsDefault);
            }
        }

        public override void OnSaving()
        {
            base.OnSaving();
        }
    }

    [DefaultClassOptions]
    [XafDisplayName("External Arrival")]
    public class ExternalArrival : TravelHistory
    {
        public override void OnCreated()
        {
            base.OnCreated();
            TravelType = BusinessObjects.TravelType.External;
            MovementType = BusinessObjects.MovementType.Entry;
        }
    }

    [DefaultClassOptions]
    [XafDisplayName("External Departure")]
    public class ExternalDeparture : TravelHistory
    {
        public override void OnCreated()
        {
            base.OnCreated();
            TravelType = BusinessObjects.TravelType.External;
            MovementType = BusinessObjects.MovementType.Exit;
        }
    }

    [DefaultClassOptions]
    [XafDisplayName("Internal Arrival")]
    public class InternalArrival : TravelHistory
    {
        public override void OnCreated()
        {
            base.OnCreated();
            TravelType = BusinessObjects.TravelType.Internal;
            MovementType = BusinessObjects.MovementType.Entry;
        }
    }

    [DefaultClassOptions]
    [XafDisplayName("Internal Departure")]
    public class InternalDeparture : TravelHistory
    {
        public override void OnCreated()
        {
            base.OnCreated();
            TravelType = BusinessObjects.TravelType.Internal;
            MovementType = BusinessObjects.MovementType.Exit;
        }
    }
}
