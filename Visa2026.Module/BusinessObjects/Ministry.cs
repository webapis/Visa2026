using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]
    [DefaultProperty(nameof(Name))]
    public class Ministry : BaseObject
    {
        /// <summary>
        /// Short display name of the ministry or organization.
        /// Example: "Türkmenenergo"
        /// </summary>
        [MaxLength(100)]
        public virtual string Name { get; set; }

        /// <summary>
        /// Full recipient block as it appears in the letter header — stored as RTF.
        /// Edited visually in XAF using the RichEdit property editor (bold, underline, font size preserved).
        /// Bound directly to XRRichText in reports — no assembly needed.
        /// Example content (formatted):
        ///   "Türkmenenergo" döwlet elektroenergetika
        ///   korporasiýasynyň başlygy
        ///   D. Elyasowa
        /// </summary>
        [MaxLength(500)]
        public virtual string RecipientBlock { get; set; }

        /// <summary>
        /// Salutation line used at the opening of the letter body.
        /// Example: "Hormatly Durdy Baýjanowiç!"
        /// </summary>
        [MaxLength(200)]
        public virtual string FormOfAddress { get; set; }
    }
}
