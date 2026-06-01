using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
  
    [DefaultProperty(nameof(State))]
    //[RuleCriteria("ApplicationProgressDateNotInFuture", DefaultContexts.Save, "Date <= Now()", "Date cannot be in the future.")]
    //[RuleCriteria("ApplicationProgressDateNotBeforeApplicationDate", DefaultContexts.Save, "Date >= Application.ApplicationDate", "Progress date cannot be earlier than the application date.")]
    public class ApplicationProgress : BaseObject
    {
        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [DataSourceProperty(nameof(AvailableStatesForNextStep))]
        public virtual ApplicationState State { get; set; }

        [RuleRequiredField]
        [DataSourceProperty(nameof(AvailableLocationsForSelectedState))]
        public virtual ApplicationLocation Location { get; set; }

        [Browsable(false)]
        [NotMapped]
        public IList<ApplicationState> AvailableStatesForNextStep => LoadAvailableStatesForNextStep();

        [Browsable(false)]
        [NotMapped]
        public IList<ApplicationLocation> AvailableLocationsForSelectedState => LoadAvailableLocationsForSelectedState();

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime Date { get; set; }


        [MaxLength(255)]
        public virtual string Description { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            Date = DateTime.Now;
            ApplicationProgressTransitionHelper.TryApplySuggestedNextStep(this);
        }

        private IList<ApplicationState> LoadAvailableStatesForNextStep()
        {
            var objectSpace = ObjectSpaceHelper.Get(this) ?? ObjectSpaceHelper.Get(Application);
            if (objectSpace == null || Application == null)
                return Array.Empty<ApplicationState>();

            var allowedCodes = ApplicationProgressTransitionHelper
                .GetAllowedStateCodesForProgressRow(this, objectSpace)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return objectSpace.GetObjectsQuery<ApplicationState>()
                .Where(s => s.Code != null && allowedCodes.Contains(s.Code))
                .OrderBy(s => s.Code)
                .ToList();
        }

        private IList<ApplicationLocation> LoadAvailableLocationsForSelectedState()
        {
            var objectSpace = ObjectSpaceHelper.Get(this) ?? ObjectSpaceHelper.Get(Application);
            if (objectSpace == null || Application == null)
                return Array.Empty<ApplicationLocation>();

            var allowedCodes = ApplicationProgressTransitionHelper
                .GetAllowedLocationCodesForProgressRow(this, objectSpace)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return objectSpace.GetObjectsQuery<ApplicationLocation>()
                .Where(l => l.Code != null && allowedCodes.Contains(l.Code))
                .OrderBy(l => l.Code)
                .ToList();
        }
    }
}