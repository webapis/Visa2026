

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
    public class ContractTemplate : BaseObject, ISoftDelete, IObjectSpaceLink
    {
        public virtual string TemplateName { get; set; }

        public virtual bool IsDefault { get; set; }

       [FieldSize(FieldSizeAttribute.Unlimited)]
        [EditorAlias("RichText")]
        public virtual string Content { get; set; }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        public override void OnSaving()
        {
            base.OnSaving();
            if (ObjectSpace != null && IsDefault)
            {
                var otherDefaults = ObjectSpace.GetObjectsQuery<ContractTemplate>()
                    .Where(t => t.ID != this.ID && t.IsDefault)
                    .ToList();
                
                foreach (var other in otherDefaults)
                {
                    other.IsDefault = false;
                }
            }
        }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion
    }
}