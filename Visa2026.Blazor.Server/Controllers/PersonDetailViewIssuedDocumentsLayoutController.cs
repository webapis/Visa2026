using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Layout;
using Visa2026.Module;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Hides issued-document tabs for new <see cref="Person"/> rows and shows a short onboarding hint instead.
/// </summary>
public sealed class PersonDetailViewIssuedDocumentsLayoutController : ObjectViewController<DetailView, Person>
{
    private DxFormLayoutTabPagesModel? _issuedDocumentsTabs;
    private DxFormLayoutTabPagesModel? _personRecordTabs;
    private DxFormLayoutGroupModel? _newRecordHint;

    public PersonDetailViewIssuedDocumentsLayoutController()
    {
        TargetViewId = PersonNestedCollectionLayout.TypedDetailViewIds;
    }

    private string ResolveNewRecordHintKey() =>
        View.Id == PersonDetailViewIds.TemporaryVisitor
            ? "Person.DetailSection.NewRecordIssuedHint.Visitor"
            : "Person.DetailSection.NewRecordIssuedHint";

    protected override void OnActivated()
    {
        base.OnActivated();
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated += OnLayoutItemCreated;

        View.CurrentObjectChanged += OnCurrentObjectChanged;
        ApplyIssuedDocumentsChrome();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyIssuedDocumentsChrome();
    }

    protected override void OnDeactivated()
    {
        View.CurrentObjectChanged -= OnCurrentObjectChanged;
        if (View.LayoutManager is BlazorLayoutManager layoutManager)
            layoutManager.ItemCreated -= OnLayoutItemCreated;

        _issuedDocumentsTabs = null;
        _personRecordTabs = null;
        _newRecordHint = null;
        base.OnDeactivated();
    }

    private void OnCurrentObjectChanged(object? sender, EventArgs e) => ApplyIssuedDocumentsChrome();

    private void OnLayoutItemCreated(object? sender, BlazorLayoutManager.ItemCreatedEventArgs e)
    {
        if (e.ModelLayoutElement.Id == PersonNestedCollectionLayout.IssuedDocumentsTabs
            && e.LayoutControlItem is DxFormLayoutTabPagesModel issuedTabs)
        {
            _issuedDocumentsTabs = issuedTabs;
            issuedTabs.Caption = VisaUiMessages.Get("Person.DetailSection.IssuedDocuments");
        }
        else if (e.ModelLayoutElement.Id == PersonNestedCollectionLayout.PersonRecordTabs
            && e.LayoutControlItem is DxFormLayoutTabPagesModel recordTabs)
        {
            _personRecordTabs = recordTabs;
            recordTabs.Caption = VisaUiMessages.Get("Person.DetailSection.PersonRecordData");
        }
        else if (e.ModelLayoutElement.Id == PersonNestedCollectionLayout.PersonNewRecordIssuedHint
            && e.LayoutControlItem is DxFormLayoutGroupModel hintGroup)
        {
            _newRecordHint = hintGroup;
            hintGroup.Caption = VisaUiMessages.Get(ResolveNewRecordHintKey());
        }

        ApplyIssuedDocumentsChrome();
    }

    private void ApplyIssuedDocumentsChrome()
    {
        bool isNew = View.ObjectSpace.IsNewObject(View.CurrentObject);
        if (_issuedDocumentsTabs != null)
            _issuedDocumentsTabs.Visible = !isNew;
        if (_newRecordHint != null)
            _newRecordHint.Visible = isNew;
    }
}