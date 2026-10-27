using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.Email;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Module.Controllers;

/// <summary>
/// <b>Email copies</b> on Employee and Family member Person lists (selected people) and on the
/// matching Person detail views, plus <b>Resend</b> on <see cref="SentEmail"/>. Opens the centered
/// popup. See <c>docs/EMAIL_DOCUMENT_COPIES.md</c>.
/// </summary>
public sealed class EmailDocumentCopiesController : ViewController
{
    public const string EmployeesListViewId = "Person_ListView_Employees";
    public const string FamilyMembersListViewId = "Person_ListView_FamilyMembers";

    private static readonly string PersonTargetViewIds = string.Join(';', new[]
    {
        EmployeesListViewId,
        FamilyMembersListViewId,
        PersonDetailViewIds.Employee,
        PersonDetailViewIds.FamilyMember,
        PersonDetailViewIds.Default,
    });

    private readonly SimpleAction emailCopiesAction;
    private readonly SimpleAction resendAction;

    public EmailDocumentCopiesController()
    {
        emailCopiesAction = new SimpleAction(this, "EmailDocumentCopies", PredefinedCategory.View)
        {
            ImageName = "Actions_Send",
            SelectionDependencyType = SelectionDependencyType.RequireMultipleObjects,
        };
        emailCopiesAction.Execute += EmailCopiesAction_Execute;

        resendAction = new SimpleAction(this, "ResendSentEmail", PredefinedCategory.View)
        {
            ImageName = "Actions_Send",
            SelectionDependencyType = SelectionDependencyType.RequireSingleObject,
        };
        resendAction.Execute += ResendAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        var type = View.ObjectTypeInfo?.Type;
        bool canSend = CanSend();
        bool personView = View.Id != null && PersonTargetViewIds.Split(';').Contains(View.Id);

        emailCopiesAction.Caption = VisaUiMessages.Get("EmailDocumentCopies.Action");
        emailCopiesAction.Active["SupportedType"] = personView && type != null && typeof(Person).IsAssignableFrom(type);
        emailCopiesAction.Active["Permission"] = canSend;

        resendAction.Caption = VisaUiMessages.Get("EmailDocumentCopies.Action.Resend");
        resendAction.Active["SupportedType"] = type != null && typeof(SentEmail).IsAssignableFrom(type);
        resendAction.Active["Permission"] = canSend;

        View.SelectionChanged += View_SelectionChanged;
        UpdateEnabled();
    }

    protected override void OnDeactivated()
    {
        View.SelectionChanged -= View_SelectionChanged;
        base.OnDeactivated();
    }

    private void View_SelectionChanged(object? sender, EventArgs e) => UpdateEnabled();

    private void UpdateEnabled()
    {
        bool isNew = View is DetailView detail && detail.CurrentObject != null && ObjectSpace.IsNewObject(detail.CurrentObject);
        emailCopiesAction.Enabled["Saved"] = !isNew;
    }

    private bool CanSend()
    {
        try
        {
            return SecuritySystem.Instance == null
                || SecuritySystem.IsGranted(new PermissionRequest(ObjectSpace, typeof(SentEmail), SecurityOperations.Create));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void EmailCopiesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var personIds = e.SelectedObjects
            .OfType<Person>()
            .Select(p => p.ID)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (personIds.Count == 0)
        {
            Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("EmailDocumentCopies.Error.NoPeople"), InformationType.Warning);
            return;
        }

        Open(new EmailDocumentCopiesDialogRequest
        {
            PersonIds = personIds,
            SourceDescription = VisaUiMessages.Format("EmailDocumentCopies.Source.People", personIds.Count),
        });
    }

    private void ResendAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (e.CurrentObject is not SentEmail sent)
            return;

        var persons = sent.Persons?
            .Where(p => p?.Person != null && !string.IsNullOrWhiteSpace(p.RecordKeys))
            .GroupBy(p => p.Person.ID)
            .Select(g => new EmailDocumentCopiesPersonSelection
            {
                PersonId = g.Key,
                RecordKeys = g.SelectMany(p => p.RecordKeys.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).Distinct(StringComparer.Ordinal).ToList(),
            })
            .ToList() ?? new List<EmailDocumentCopiesPersonSelection>();

        if (persons.Count == 0)
        {
            Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("EmailDocumentCopies.Error.NoPeople"), InformationType.Warning);
            return;
        }

        Open(new EmailDocumentCopiesDialogRequest
        {
            PersonIds = persons.Select(p => p.PersonId).ToList(),
            FixedRecords = persons,
            SourceDescription = sent.SourceDescription,
            ResendFromSentEmailId = sent.ID,
        });
    }

    private void Open(EmailDocumentCopiesDialogRequest request)
    {
        var dialog = Application.ServiceProvider?.GetService<IEmailDocumentCopiesDialogService>();
        if (dialog == null)
        {
            Application.ShowViewStrategy.ShowMessage(VisaUiMessages.Get("EmailDocumentCopies.Error.Unavailable"), InformationType.Error);
            return;
        }

        dialog.Open(request);
    }
}
