using System;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Visa2026.Module.Editors;

namespace Visa2026.Module.BusinessObjects.ApplicationProfileWizard;

/// <summary>
/// Non-persistent shell for the Application Profile configuration wizard DetailView.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Configure Application Profile")]
[ImageName("BO_List")]
public class ApplicationProfileWizardHost : NonPersistentBaseObject
{
    [Browsable(false)]
    public Guid ApplicationProfileId { get; set; }

    [VisibleInListView(false)]
    [VisibleInLookupListView(false)]
    [ModelDefault("ShowCaption", "False")]
    [EditorAlias(ApplicationProfileWizardEditorAliases.Wizard)]
    public string WizardUi { get; set; } = string.Empty;
}
