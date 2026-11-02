using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using Microsoft.AspNetCore.Components.Rendering;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Model;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// Stacks each linked person with a circle per result that application produces.
/// </summary>
public sealed class ApplicationListViewLinkedPeopleController : ViewController<ListView>
{
    private CancellationTokenSource? deferredApplyCts;

    public ApplicationListViewLinkedPeopleController()
    {
        TargetObjectType = typeof(ApplicationProfileInstance);
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        ApplyPeopleTemplate();
        ScheduleDeferredApply();
    }

    private void ScheduleDeferredApply()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = new CancellationTokenSource();
        var token = deferredApplyCts.Token;
        _ = ApplyDeferredAsync(token);
    }

    private async Task ApplyDeferredAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(150, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (View is { IsDisposed: false })
            ApplyPeopleTemplate();
    }

    protected override void OnDeactivated()
    {
        deferredApplyCts?.Cancel();
        deferredApplyCts?.Dispose();
        deferredApplyCts = null;
        base.OnDeactivated();
    }

    internal void ApplyPeopleTemplate()
    {
        if (View?.Editor is not DxGridListEditor gridListEditor)
            return;

        foreach (var columnModel in gridListEditor.GridDataColumnModels)
        {
            if (!string.Equals(
                    columnModel.FieldName,
                    nameof(ApplicationProfileInstance.LinkedPeopleDisplay),
                    StringComparison.Ordinal))
                continue;

            columnModel.MinWidth = 220;
            if (string.IsNullOrEmpty(columnModel.Name))
                columnModel.Name = ApplicationProfileInstanceLinkedPeopleColumnUpdater.ColumnId;

            columnModel.CellDisplayTemplate = context => builder =>
            {
                if (context.DataItem is not ApplicationProfileInstance application)
                {
                    builder.AddContent(0, "\u2014");
                    return;
                }

                var people = application.ListViewLinkedPeople;
                if (people.Count == 0)
                {
                    people = Frame.GetController<ApplicationListViewPreloadController>()
                        ?.LinkedPeopleFor(application.ID) ?? people;
                }

                if (people.Count == 0)
                {
                    builder.AddContent(0, string.IsNullOrWhiteSpace(application.LinkedPeopleDisplay)
                        ? "\u2014"
                        : application.LinkedPeopleDisplay);
                    return;
                }

                RenderPeople(builder, people);
            };
        }
    }

    private static void RenderPeople(
        RenderTreeBuilder builder,
        IReadOnlyList<ApplicationListLinkedPeople.Row> people)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "lv-people");
        var seq = 2;
        foreach (var person in people)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "lv-people__row");
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "lv-people__name");
            builder.AddContent(seq++, person.Name);
            builder.CloseElement();

            if (person.Marks.Count > 0)
            {
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "lv-people__marks");
                foreach (var mark in person.Marks)
                {
                    builder.OpenElement(seq++, "span");
                    builder.AddAttribute(seq++, "class", "lv-person-mark lv-person-mark--" + mark.Tone);
                    builder.AddAttribute(seq++, "title", ApplicationListLinkedPeople.Title(mark));
                    builder.AddContent(seq++, ApplicationListLinkedPeople.Letters(mark.Key));
                    builder.CloseElement();
                }

                builder.CloseElement();
            }

            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
