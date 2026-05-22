using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Rejection")]
    [DefaultProperty(nameof(RejectionTitle))]
    public class Rejection : BaseObject, IPersonLinkParent
    {
        public Rejection()
        {
            Images = new ObservableCollection<RejectionImage>();
            Documents = new ObservableCollection<RejectionDocument>();
            RejectionItems = new ObservableCollection<RejectionItem>();
        }

        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [MaxLength(50)]
        public virtual string RejectedDocNumber { get; set; }

        public virtual string Reason { get; set; }

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime Date { get; set; }

        [InverseProperty(nameof(RejectionImage.Rejection))]
        [DevExpress.ExpressApp.DC.Aggregated]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<RejectionImage> Images { get; set; }

        [InverseProperty(nameof(RejectionDocument.Rejection))]
        [DevExpress.ExpressApp.DC.Aggregated]
        public virtual IList<RejectionDocument> Documents { get; set; }
        
        [InverseProperty(nameof(RejectionItem.Rejection))]
        [DevExpress.ExpressApp.DC.Aggregated]
        public virtual IList<RejectionItem> RejectionItems { get; set; }

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Person> AvailablePeople
        {
            get
            {
                return Application?.ApplicationItems.Select(ai => ai.Person).ToList() ?? new List<Person>();
            }
        }

        [NotMapped]
        public string RejectionTitle => VisaUiMessages.Format(
            "Rejection.DisplayTitle",
            RejectedDocNumber ?? string.Empty,
            Date.ToString("d", CultureInfo.CurrentUICulture));
    }
}