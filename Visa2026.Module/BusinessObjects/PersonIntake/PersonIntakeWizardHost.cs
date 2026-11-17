using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.PersonIntake;

/// <summary>
/// Non-persistent shell for the person intake wizard. Same host pattern as <c>ApplicationProfileWizardHost</c>:
/// the DetailView shows only <see cref="WizardUi"/>, which the Blazor editor renders as custom HTML.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Person intake")]
[ImageName("BO_Person")]
public class PersonIntakeWizardHost : NonPersistentBaseObject
{
    [Browsable(false)]
    public Guid PersonId { get; set; }

    [Browsable(false)]
    public PersonRecordRole Role { get; set; }

    /// <summary>Person being edited. Lives in <see cref="PersonObjectSpace"/>, not this host object space.</summary>
    [Browsable(false)]
    public Person? Person { get; set; }

    [Browsable(false)]
    public IObjectSpace? PersonObjectSpace { get; set; }

    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("AllowEdit", "True")]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(PersonIntakeWizardEditorAliases.Wizard)]
    public string WizardUi { get; set; } = string.Empty;
}
