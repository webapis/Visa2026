using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]
    [DefaultProperty(nameof(Name))]
    public class Ministry : BaseObject
    {
  

        [MaxLength(100)]
        public virtual string Name { get; set; }

        [MaxLength(100)]
        public virtual string TitleOfMinisteryL { get; set; }

        [MaxLength(100)]
        public virtual string MinisterPosition { get; set; }

        [MaxLength(100)]
        public virtual string MinisterFullName { get; set; }

        [MaxLength(100)]
        public virtual string formOfAddress { get; set; }

     
    }
}
