using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Validation;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects {
    [DefaultClassOptions]
    [NavigationItem(false)]
    public class WorkPermitItemPermittedCity : BaseObject, IObjectSpaceLink {
        public WorkPermitItemPermittedCity() {
            ShowMostlyUsedOnly = true;
        }

        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [Browsable(false)]
        public virtual Guid WorkPermitItemId { get; set; }

        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [Browsable(false)]
        public virtual WorkPermitItem WorkPermitItem { get; set; }

        [ImmediatePostData]
        [XafDisplayName("Mostly Used only")]
        [VisibleInListView(false)]
        public virtual bool ShowMostlyUsedOnly { get; set; }

        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [Browsable(false)]
        public virtual Guid CityId { get; set; }

        [RuleRequiredField]
        [VisibleInDetailView(true)]
        [VisibleInListView(false)]
        [ImmediatePostData]
        [DataSourceProperty(nameof(AvailableCities))]
        [EditorAlias("CityLookupAutoClear")]
        public virtual City City { get; set; }

        [NotMapped]
        [Browsable(false)]
        public IList<City> AvailableCities {
            get {
                if (ObjectSpace == null) {
                    return Array.Empty<City>();
                }

                var qAll = ObjectSpace.GetObjectsQuery<City>();
                var q = ShowMostlyUsedOnly ? qAll.Where(c => c.IsMostlyUsed) : qAll;

                var list = q.OrderByDescending(c => c.IsMostlyUsed).ThenBy(c => c.NameTm).ToList();

                // If there are no mostly-used cities yet, don't show an empty lookup.
                if (ShowMostlyUsedOnly && list.Count == 0) {
                    list = qAll.OrderByDescending(c => c.IsMostlyUsed).ThenBy(c => c.NameTm).ToList();
                }

                // Ensure currently selected city stays selectable even when filtered.
                if (City != null && list.All(c => c.ID != City.ID)) {
                    list.Insert(0, City);
                }

                return list;
            }
        }

        // Convenience for list views / sorting without deep property paths.
        [NotMapped]
        public string CityNameTm => City?.NameTm;

        [NotMapped]
        [XafDisplayName("Region")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        public string RegionNameTm => City?.Region?.NameTm;

        public override void OnSaving() {
            base.OnSaving();
            if (City != null && !City.IsMostlyUsed) {
                City.IsMostlyUsed = true;
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
    }
}

