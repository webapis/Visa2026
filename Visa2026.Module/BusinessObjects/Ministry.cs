using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Editors;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]
    [DefaultProperty(nameof(Name))]
    public class Ministry : BaseObject
    {
        public Ministry()
        {
            ProjectContracts = new ObservableCollection<ProjectContract>();
        }

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
        [EditorAlias(EditorAliases.RichTextPropertyEditor)]
        public virtual string RecipientBlock { get; set; }

        /// <summary>
        /// Salutation line used at the opening of the letter body.
        /// Example: "Hormatly Durdy Baýjanowiç!"
        /// </summary>
        [MaxLength(200)]
        public virtual string FormOfAddress { get; set; }

        [InverseProperty(nameof(ProjectContract.Ministry))]
        public virtual IList<ProjectContract> ProjectContracts { get; set; }

    }
}
