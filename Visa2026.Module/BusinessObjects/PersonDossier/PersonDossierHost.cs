using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.PersonDossier;

/// <summary>
/// Non-persistent shell for the read-only person dossier page.
/// </summary>
/// <remarks>
/// The dossier lives in the main content area rather than the preview slot: the slot allows one
/// occupant at a time, so hosting the dossier there would evict it as soon as the officer opened
/// document copies. See <c>docs/PERSON_DOSSIER.md</c>.
/// </remarks>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Person Dossier")]
[ImageName("BO_Person")]
public class PersonDossierHost : NonPersistentBaseObject
{
    /// <summary>Person the dossier renders; set before the detail view is created.</summary>
    [Browsable(false)]
    public Guid PersonId { get; set; }

    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(PersonDossierEditorAliases.Dossier)]
    public string DossierUi { get; set; } = string.Empty;
}
