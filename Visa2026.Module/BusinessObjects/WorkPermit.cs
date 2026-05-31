using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("WorkPermit")]
    [SupportsOptionalDetailFields]
    public class WorkPermit : BaseObject, ISoftDelete, IOptionalDetailFields
    {
        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [XafDisplayName("Issued Date")]
        [Column("StartDate")]
        public virtual DateTime IssuedDate { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        [XafDisplayName(" ")]
        public bool ShowOptionalFields { get; set; }

        /// <summary>
        /// Optional link to a visa application. When set, work permit items can be validated against that application's people.
        /// </summary>
        [ImmediatePostData]
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        [VisibleInLookupListView(false)]
        [ToolTip("Link this work permit to an application when one exists. Leave empty for standalone work permits.")]
        public virtual Application Application { get; set; }

        [Aggregated]
        public virtual IList<WorkPermitItem> WorkPermitItems { get; set; } = new ObservableCollection<WorkPermitItem>();

        [Aggregated]
        public virtual IList<WorkPermitDocument> Documents { get; set; } = new ObservableCollection<WorkPermitDocument>();

        [Aggregated]
        [InverseProperty(nameof(WorkPermitImage.WorkPermit))]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<WorkPermitImage> Images { get; set; } = new ObservableCollection<WorkPermitImage>();

        [NotMapped]
        [Browsable(false)]
        public virtual IList<Person> AvailableEmployees
        {
            get
            {
                if (Application?.ApplicationItems != null)
                {
                    return Application.ApplicationItems
                        .Select(ai => ai.Person)
                        .Where(p => p != null && p.IsEmployee)
                        .ToList()!;
                }

                IObjectSpace? objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                {
                    return Array.Empty<Person>();
                }

                return objectSpace.GetObjectsQuery<Person>()
                    .Where(p => !p.IsDeleted && !p.IsArchived && p.IsEmployee)
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToList();
            }
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}