

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.Linq;
using DevExpress.ExpressApp;








namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(TemplateName))]
    public class ContractTemplate  : BaseObject
    {
        public virtual string TemplateName { get; set; }

        public virtual bool IsDefault { get; set; }

       [FieldSize(FieldSizeAttribute.Unlimited)]
        [EditorAlias("RichText")]
        public virtual string Content { get; set; }


        public override void OnSaving()
        {
            base.OnSaving();
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null && IsDefault)
            {
                var otherDefaults = objectSpace.GetObjectsQuery<ContractTemplate>()
                    .Where(t => t.ID != this.ID && t.IsDefault)
                    .ToList();
                
                foreach (var other in otherDefaults)
                {
                    other.IsDefault = false;
                }
            }
        }
    }
}