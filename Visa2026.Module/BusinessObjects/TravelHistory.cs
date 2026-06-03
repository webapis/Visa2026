using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using System.Linq;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem(false)]
    [DefaultProperty(nameof(Title))]
    [Appearance("TravelHistoryManagedByApplicationItem", Enabled = false,
        Criteria = "SourceApplicationItem is not null", Context = "DetailView", TargetItems = "*")]
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

        /// <summary>
        /// When set, this row is maintained from a registration <see cref="ApplicationItem"/> (check-in/out types).
        /// Manual edits on the person travel list are disabled in the UI.
        /// </summary>
        [Browsable(false)]
        public virtual Guid? SourceApplicationItemID { get; set; }

        [ForeignKey(nameof(SourceApplicationItemID))]
        [Browsable(false)]
        public virtual ApplicationItem SourceApplicationItem { get; set; }

        /// <summary>Parent application number when this row is synced from a registration <see cref="ApplicationItem"/>.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [XafDisplayName("Application Number")]
        public string SourceApplication_FullApplicationNumber =>
            SourceApplicationItem?.Application?.FullApplicationNumber;

        /// <summary>Parent application date when this row is synced from a registration <see cref="ApplicationItem"/>.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [XafDisplayName("Application Date")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        public DateTime? SourceApplication_ApplicationDate =>
            SourceApplicationItem?.Application?.ApplicationDate;

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
