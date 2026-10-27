using System.Globalization;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Models;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonList;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>Employees, family members, and temporary visitors lists.</summary>
public static class PersonRosterListViews
{
    public static readonly string[] Ids =
    [
        "Person_ListView_Employees",
        "Person_ListView_FamilyMembers",
        "Person_ListView_TemporaryVisitors",
    ];

    public static bool IsRoster(string? viewId) =>
        Ids.Any(id => string.Equals(id, viewId, StringComparison.Ordinal));
}

/// <summary>
/// Person lists: stacked in-force visas, work permits, and invitations, with the
/// number opening that document's copy. Each column keeps its own pixel width.
/// A drag is stored on the list view in the user model differences, the same
/// store XAF uses for UI state.
/// </summary>
public sealed class PersonListViewActiveDocumentsController : ViewController<ListView>
{
    private static readonly string[] CopyFields =
    [
        nameof(Person.EmployeeListActivePassport),
        nameof(Person.EmployeeListEducationPlace),
        nameof(Person.EmployeeListEducationProfession),
        nameof(Person.EmployeeListResidenceAddress),
        nameof(Person.EmployeeListCvCopies),
        nameof(Person.EmployeeListFamilyProofCopies),
    ];

    private static readonly string[] DocumentFields =
    [
        nameof(Person.EmployeeListActiveVisaNumbers),
        nameof(Person.EmployeeListActiveWorkPermitNumbers),
        nameof(Person.EmployeeListActiveInvitationNumbers),
    ];

    private static readonly HashSet<string> FixedFields = new(StringComparer.Ordinal)
    {
        nameof(Person.DossierListLink),
        nameof(Person.DocumentCopiesListLink),
    };

    /// <summary>First-paint weights, used until the browser measures real cell text.</summary>
    private static readonly Dictionary<string, int> FallbackPreferred = new(StringComparer.Ordinal)
    {
        [nameof(Person.DossierListLink)] = 8,
        [nameof(Person.DocumentCopiesListLink)] = 8,
        [nameof(Person.FullName)] = 170,
        [nameof(Person.EmployeeListActivePassport)] = 120,
        [nameof(Person.PersonalNumber)] = 100,
        [nameof(Person.DateOfBirth)] = 92,
        [nameof(Person.Age)] = 36,
        [nameof(Person.Gender)] = 52,
        [nameof(Person.MaritalStatus)] = 80,
        [nameof(Person.Nationality)] = 72,
        [nameof(Person.ProjectContract)] = 120,
        [nameof(Person.Subcontractor)] = 140,
        [nameof(Person.EmployeeListPositionName)] = 130,
        [nameof(Person.EmployeeListActualPositionName)] = 140,
        [nameof(Person.EmployeeListDepartmentName)] = 100,
        [nameof(Person.EmployeeListActiveVisaNumbers)] = 160,
        [nameof(Person.EmployeeListActiveWorkPermitNumbers)] = 150,
        [nameof(Person.EmployeeListActiveInvitationNumbers)] = 160,
        [nameof(Person.EmployeeListResidenceAddress)] = 320,
        [nameof(Person.EmployeeListCvCopies)] = 160,
        [nameof(Person.EmployeeListFamilyProofCopies)] = 160,
        [nameof(Person.EmployeeListEducationPlace)] = 170,
        [nameof(Person.EmployeeListEducationProfession)] = 150,
        [nameof(Person.Relationship)] = 90,
        [nameof(Person.SponsoringEmployee)] = 120,
        [nameof(Person.IsArchived)] = 64,
    };

    private DotNetObjectReference<PersonListViewActiveDocumentsController>? dotNetRef;
    private CancellationTokenSource? applyCts;
    private int applyGate;
    private int pendingRerun;
    private bool widthsCustomized;
    private bool userResized;
    private int resizeCommitPending;
    private int applyingWidths;
    private int lastViewportApplied;
    private int[] lastAppliedPixels = Array.Empty<int>();

    public PersonListViewActiveDocumentsController()
    {
        TargetObjectType = typeof(Person);
        TargetViewId = string.Join(';', PersonRosterListViews.Ids);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        var changed = HideColumn(nameof(Person.Gender));
        if (string.Equals(View.Id, "Person_ListView_Employees", StringComparison.Ordinal))
        {
            changed |= HideColumn(nameof(Person.PersonalNumber));
            changed |= HideColumn(nameof(Person.ProjectContract));
        }

        if (changed)
            PersistLayout();
    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        widthsCustomized = View.Model is IModelPersonRosterLayout layout && layout.ColumnWidthsCustomized;
        ApplyPresentation();
        ApplyFixedPixelWidths();
        applyCts?.Cancel();
        applyCts?.Dispose();
        applyCts = new CancellationTokenSource();
        _ = StartAsync(applyCts.Token);
    }

    protected override void OnDeactivated()
    {
        applyCts?.Cancel();
        applyCts?.Dispose();
        applyCts = null;
        _ = ReleaseJsAsync();
        dotNetRef?.Dispose();
        dotNetRef = null;
        base.OnDeactivated();
    }

    [JSInvokable]
    public void OnColumnResizeStarting()
    {
        resizeCommitPending = 1;
    }

    [JSInvokable]
    public void OnColumnResizeCancelled()
    {
        resizeCommitPending = 0;
    }

    [JSInvokable]
    public void OnOneColumnResized(int headerIndex, int headerCount, int pixels)
    {
        resizeCommitPending = 0;
        if (View is not { IsDisposed: false, Model: not null } || pixels <= 0 || headerCount <= 0)
            return;
        if (View.Editor is not DxGridListEditor editor)
            return;

        var columns = VisibleColumns(editor);
        var dataIndex = headerIndex - (headerCount - columns.Count);
        if (dataIndex < 0 || dataIndex >= columns.Count)
            return;

        var field = columns[dataIndex].FieldName ?? string.Empty;
        if (field.Length == 0)
            return;

        var modelColumn = View.Model.Columns[field];
        if (modelColumn != null && modelColumn.Width != pixels)
            modelColumn.Width = pixels;

        if (View.Model is IModelPersonRosterLayout layout && !layout.ColumnWidthsCustomized)
            layout.ColumnWidthsCustomized = true;

        widthsCustomized = true;
        userResized = true;

        var locked = new List<PersonListColumnShare.Width>(columns.Count);
        for (var i = 0; i < columns.Count; i++)
        {
            var columnField = columns[i].FieldName ?? string.Empty;
            var kept = i == dataIndex
                ? pixels
                : i < lastAppliedPixels.Length && lastAppliedPixels[i] > 0
                    ? lastAppliedPixels[i]
                    : View.Model.Columns[columnField]?.Width ?? pixels;
            locked.Add(new PersonListColumnShare.Width(columnField, Math.Max(1, kept)));
        }

        ApplyPixelWidths(editor, columns, locked);
        PersistLayout();
    }

    [JSInvokable]
    public void OnColumnWidthsCommitted(int[]? headerWidths)
    {
        resizeCommitPending = 0;
        if (View is not { IsDisposed: false } || headerWidths == null || headerWidths.Length == 0)
            return;
        if (View.Editor is not DxGridListEditor editor)
            return;

        var columns = VisibleColumns(editor);
        if (headerWidths.Length < columns.Count || columns.Count == 0)
            return;

        var start = headerWidths.Length - columns.Count;
        var widths = new Dictionary<string, int>(StringComparer.Ordinal);
        var moved = false;
        for (var i = 0; i < columns.Count; i++)
        {
            var pixels = Math.Max(1, headerWidths[start + i]);
            var field = columns[i].FieldName ?? string.Empty;
            if (field.Length > 0)
                widths[field] = pixels;
            var previous = i < lastAppliedPixels.Length ? lastAppliedPixels[i] : 0;
            if (previous <= 0 || Math.Abs(previous - pixels) >= 4)
                moved = true;
        }

        if (moved)
            CommitWidths(widths);
    }

    [JSInvokable]
    public Task OnRosterWidthChanged() => Task.CompletedTask;

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(350, cancellationToken);
            if (View is not { IsDisposed: false })
                return;

            ApplyPresentation();
            ApplyFixedPixelWidths();

            var js = GetJs();
            if (js == null || cancellationToken.IsCancellationRequested || View is not { IsDisposed: false })
                return;

            dotNetRef ??= DotNetObjectReference.Create(this);
            await js.InvokeVoidAsync("visaPersonRoster.observe", cancellationToken, dotNetRef);
        }
        catch (OperationCanceledException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void ApplyPresentation()
    {
        if (View?.Editor is not DxGridListEditor editor)
            return;

        editor.GridModel.TextWrapEnabled = true;
        editor.GridModel.ColumnResizeMode = GridColumnResizeMode.ColumnsContainer;
        editor.GridModel.LayoutAutoSaving = OnLayoutSaving;
        editor.GridModel.CssClass = AppendCssClass(editor.GridModel.CssClass, "visa-person-roster");

        var ownerViewId = View.Id ?? string.Empty;
        foreach (var column in editor.GridDataColumnModels)
        {
            var fieldName = column.FieldName ?? string.Empty;
            if (CopyFields.Contains(fieldName, StringComparer.Ordinal))
            {
                var copyField = fieldName;
                column.CellDisplayTemplate = context => builder =>
                    RenderCopyCells(builder, context.DataItem as Person, copyField, ownerViewId);
                continue;
            }

            if (!DocumentFields.Contains(fieldName, StringComparer.Ordinal))
                continue;

            var field = fieldName;
            column.CellDisplayTemplate = context => builder =>
                RenderDocuments(builder, context.DataItem as Person, field, ownerViewId);
        }
    }

    private void ApplyFixedPixelWidths()
    {
        if (View?.Editor is not DxGridListEditor editor)
            return;

        var columns = VisibleColumns(editor);
        if (columns.Count == 0)
            return;

        var widths = new List<PersonListColumnShare.Width>(columns.Count);
        foreach (var column in columns)
        {
            var field = column.FieldName ?? string.Empty;
            var pixels = 0;
            if (field.Length > 0)
                pixels = View.Model?.Columns[field]?.Width ?? 0;
            if (pixels <= 0)
                pixels = FallbackPreferred.GetValueOrDefault(field, 120);
            widths.Add(new PersonListColumnShare.Width(field, pixels));
        }

        ApplyPixelWidths(editor, columns, widths);
    }

    private void ApplyPixelWidths(
        DxGridListEditor editor,
        IReadOnlyList<DxGridDataColumnModel> columns,
        IReadOnlyList<PersonListColumnShare.Width> widths)
    {
        applyingWidths++;
        try
        {
            var applied = new int[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                var match = widths.FirstOrDefault(width => string.Equals(width.Field, columns[i].FieldName, StringComparison.Ordinal));
                applied[i] = string.IsNullOrEmpty(match.Field) ? 0 : match.Pixels;
            }

            lastAppliedPixels = applied;
            editor.BeginUpdate();
            try
            {
                foreach (var column in columns)
                {
                    var match = widths.FirstOrDefault(width => string.Equals(width.Field, column.FieldName, StringComparison.Ordinal));
                    if (string.IsNullOrEmpty(match.Field) || match.Pixels <= 0)
                        continue;

                    var text = match.Pixels.ToString(CultureInfo.InvariantCulture) + "px";
                    if (!string.Equals(column.Width, text, StringComparison.Ordinal))
                        column.Width = text;
                }
            }
            finally
            {
                editor.EndUpdate();
            }

            if (editor.GridModel.ComponentInstance is not IGrid grid)
                return;

            try
            {
                grid.BeginUpdate();
                try
                {
                    foreach (var column in grid.GetDataColumns())
                    {
                        var match = widths.FirstOrDefault(width => string.Equals(width.Field, column.FieldName, StringComparison.Ordinal));
                        if (string.IsNullOrEmpty(match.Field) || match.Pixels <= 0)
                            continue;

                        column.Width = match.Pixels.ToString(CultureInfo.InvariantCulture) + "px";
                    }
                }
                finally
                {
                    grid.EndUpdate();
                }
            }
            catch (InvalidOperationException)
            {
            }
        }
        finally
        {
            applyingWidths--;
        }
    }

    private void ApplyFallbackWidths()
    {
        if (View?.Editor is not DxGridListEditor editor)
            return;

        var columns = VisibleColumns(editor);
        if (columns.Count == 0)
            return;

        var measures = columns
            .Select(column => new PersonListColumnShare.Measure(
                column.FieldName ?? string.Empty,
                FallbackPreferred.GetValueOrDefault(column.FieldName ?? string.Empty, 96),
                FixedFields.Contains(column.FieldName ?? string.Empty)))
            .ToList();
        // Percent of the grid, leaving a slice for the selection checkbox. Pixel widths from the
        // model are wider than the pane and push the address column off screen.
        ApplyWidths(editor, columns, BalanceRoster(PersonListColumnShare.SharePixels(measures, 1000), 1000), percentBasis: 1042);
    }

    private void ApplySavedWidths(int available, int percentBasis)
    {
        if (View?.Editor is not DxGridListEditor editor)
            return;

        var columns = VisibleColumns(editor);
        if (columns.Count == 0)
            return;

        ApplyWidths(editor, columns, ScaleTo(SavedWeights(columns), available), percentBasis);
    }

    private List<PersonListColumnShare.Width> SavedWeights(IReadOnlyList<DxGridDataColumnModel> columns)
    {
        var list = new List<PersonListColumnShare.Width>(columns.Count);
        foreach (var column in columns)
        {
            var field = column.FieldName ?? string.Empty;
            var modelWidth = 0;
            if (!string.IsNullOrEmpty(field))
                modelWidth = View?.Model?.Columns[field]?.Width ?? 0;
            if (modelWidth <= 0)
                modelWidth = FallbackPreferred.GetValueOrDefault(field, 96);
            list.Add(new PersonListColumnShare.Width(field, modelWidth));
        }

        return list;
    }

    private Task OnLayoutSaving(GridPersistentLayoutEventArgs args) => Task.CompletedTask;

    private void TryCommitLiveGridWidths()
    {
        if (View?.Editor is not DxGridListEditor editor || editor.GridModel.ComponentInstance is not IGrid grid)
            return;

        var widths = ReadGridWidths(grid);
        if (widths.Count == 0 || !DiffersFromApplied(widths, 4))
            return;

        CommitWidths(widths);
    }

    private void CommitWidths(IReadOnlyDictionary<string, int> widths)
    {
        if (View is not { IsDisposed: false, Model: not null } || widths.Count == 0)
            return;
        if (View.Editor is not DxGridListEditor editor)
            return;

        var columns = VisibleColumns(editor);
        var changed = false;
        applyingWidths++;
        try
        {
            foreach (var column in columns)
            {
                var field = column.FieldName ?? string.Empty;
                if (field.Length == 0 || !widths.TryGetValue(field, out var pixels) || pixels <= 0)
                    continue;

                var modelColumn = View.Model.Columns[field];
                if (modelColumn != null && modelColumn.Width != pixels)
                {
                    modelColumn.Width = pixels;
                    changed = true;
                }

                var text = pixels.ToString(CultureInfo.InvariantCulture) + "px";
                if (!string.Equals(column.Width, text, StringComparison.Ordinal))
                    column.Width = text;
            }

            if (View.Model is IModelPersonRosterLayout layout && !layout.ColumnWidthsCustomized)
            {
                layout.ColumnWidthsCustomized = true;
                changed = true;
            }
        }
        finally
        {
            applyingWidths--;
        }

        if (!changed)
            return;

        widthsCustomized = true;
        userResized = true;
        var applied = new int[columns.Count];
        for (var i = 0; i < columns.Count; i++)
        {
            var field = columns[i].FieldName ?? string.Empty;
            applied[i] = widths.TryGetValue(field, out var pixels) ? pixels : 0;
        }

        lastAppliedPixels = applied;
        PersistLayout();
    }

    private bool DiffersFromApplied(IReadOnlyDictionary<string, int> widths, int threshold)
    {
        if (View?.Editor is not DxGridListEditor editor)
            return false;

        var columns = VisibleColumns(editor);
        if (lastAppliedPixels.Length != columns.Count)
            return true;

        for (var i = 0; i < columns.Count; i++)
        {
            var field = columns[i].FieldName ?? string.Empty;
            if (!widths.TryGetValue(field, out var pixels))
                continue;
            if (Math.Abs(lastAppliedPixels[i] - pixels) >= threshold)
                return true;
        }

        return false;
    }

    private void PersistLayout()
    {
        try
        {
            Application?.SaveModelChanges();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private Dictionary<string, int> ReadLayoutWidths(GridPersistentLayout? layout)
    {
        var widths = new Dictionary<string, int>(StringComparer.Ordinal);
        if (layout?.Columns == null)
            return widths;

        foreach (var column in layout.Columns)
        {
            if (string.IsNullOrEmpty(column.FieldName))
                continue;
            if (TryParseWidth(column.Width, out var pixels))
                widths[column.FieldName] = pixels;
        }

        return widths;
    }

    private static Dictionary<string, int> ReadGridWidths(IGrid? grid)
    {
        var widths = new Dictionary<string, int>(StringComparer.Ordinal);
        if (grid == null)
            return widths;

        foreach (var column in grid.GetDataColumns())
        {
            if (string.IsNullOrEmpty(column.FieldName))
                continue;
            if (TryParseWidth(column.Width, out var pixels))
                widths[column.FieldName] = pixels;
        }

        return widths;
    }

    private static bool TryParseWidth(string? text, out int pixels)
    {
        pixels = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        if (text.EndsWith('%'))
            return false;

        if (text.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            text = text[..^2];

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return false;

        pixels = (int)Math.Round(value);
        return pixels > 0;
    }

    private static IReadOnlyList<PersonListColumnShare.Width> ScaleTo(
        IReadOnlyList<PersonListColumnShare.Width> weights,
        int available)
    {
        var sum = 0;
        foreach (var weight in weights)
            sum += Math.Max(1, weight.Pixels);
        if (sum <= 0 || available <= 0)
            return weights;

        var scaled = new List<PersonListColumnShare.Width>(weights.Count);
        var used = 0;
        for (var i = 0; i < weights.Count; i++)
        {
            var pixels = i == weights.Count - 1
                ? Math.Max(1, available - used)
                : Math.Max(1, available * Math.Max(1, weights[i].Pixels) / sum);
            used += pixels;
            scaled.Add(new PersonListColumnShare.Width(weights[i].Field, pixels));
        }

        return scaled;
    }

    private async Task FitAsync()
    {
        if (Interlocked.CompareExchange(ref applyGate, 1, 0) != 0)
        {
            Interlocked.Exchange(ref pendingRerun, 1);
            return;
        }

        try
        {
            do
            {
                Interlocked.Exchange(ref pendingRerun, 0);
                await FitOnceAsync();
            }
            while (Interlocked.CompareExchange(ref pendingRerun, 0, 1) == 1 && View is { IsDisposed: false });
        }
        finally
        {
            Interlocked.Exchange(ref applyGate, 0);
            if (Interlocked.CompareExchange(ref pendingRerun, 0, 1) == 1 && View is { IsDisposed: false })
                _ = FitAsync();
        }
    }

    private async Task FitOnceAsync()
    {
        if (View?.Editor is not DxGridListEditor editor || View is not { IsDisposed: false })
            return;

        var js = GetJs();
        if (js == null)
            return;

        PersonListRosterMeasure? probe;
        try
        {
            var columnsForMeasure = VisibleColumns(editor);
            probe = await js.InvokeAsync<PersonListRosterMeasure>(
                "visaPersonRoster.measure",
                columnsForMeasure.Count);
        }
        catch (JSDisconnectedException)
        {
            return;
        }
        catch (JSException)
        {
            return;
        }
        catch (InvalidOperationException)
        {
            return;
        }

        if (resizeCommitPending > 0)
            return;

        if (probe == null || probe.Available < 240 || probe.Viewport < 240)
            return;

        var columns = VisibleColumns(editor);
        if (widthsCustomized)
        {
            if (lastViewportApplied > 0 && Math.Abs(probe.Viewport - lastViewportApplied) < 24)
                return;

            ApplySavedWidths(probe.Available, probe.Viewport);
            lastViewportApplied = probe.Viewport;
            return;
        }

        if (probe.Preferred == null || probe.Preferred.Length == 0)
            return;

        var preferred = probe.Preferred;
        if (preferred.Length != columns.Count)
            return;

        var measures = new List<PersonListColumnShare.Measure>(columns.Count);
        for (var i = 0; i < columns.Count; i++)
        {
            var field = columns[i].FieldName ?? string.Empty;
            measures.Add(new PersonListColumnShare.Measure(field, preferred[i], FixedFields.Contains(field)));
        }

        ApplyWidths(
            editor,
            columns,
            BalanceRoster(PersonListColumnShare.SharePixels(measures, probe.Available), probe.Available),
            percentBasis: probe.Viewport);
    }

    private static List<DxGridDataColumnModel> VisibleColumns(DxGridListEditor editor) =>
        editor.GridDataColumnModels
            .Where(column => column.Visible && column.VisibleIndex >= 0 && !string.IsNullOrEmpty(column.FieldName))
            .OrderBy(column => column.VisibleIndex)
            .ToList();

    private void ApplyWidths(
        DxGridListEditor editor,
        IReadOnlyList<DxGridDataColumnModel> columns,
        IReadOnlyList<PersonListColumnShare.Width> widths,
        int percentBasis)
    {
        if (percentBasis <= 0)
            return;

        applyingWidths++;
        try
        {
            var applied = new int[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                var match = widths.FirstOrDefault(width => string.Equals(width.Field, columns[i].FieldName, StringComparison.Ordinal));
                applied[i] = string.IsNullOrEmpty(match.Field) ? 0 : match.Pixels;
            }

            lastAppliedPixels = applied;
            editor.BeginUpdate();
            try
            {
                foreach (var column in columns)
                {
                    var match = widths.FirstOrDefault(width => string.Equals(width.Field, column.FieldName, StringComparison.Ordinal));
                    if (string.IsNullOrEmpty(match.Field))
                        continue;

                    var text = Percent(match.Pixels, percentBasis);
                    if (!string.Equals(column.Width, text, StringComparison.Ordinal))
                        column.Width = text;

                    var min = ColumnMinWidth(column.FieldName);
                    if (column.MinWidth != min)
                        column.MinWidth = min;
                }
            }
            finally
            {
                editor.EndUpdate();
            }

            if (editor.GridModel.ComponentInstance is not IGrid grid)
                return;

            try
            {
                grid.BeginUpdate();
                try
                {
                    foreach (var column in grid.GetDataColumns())
                    {
                        var match = widths.FirstOrDefault(width => string.Equals(width.Field, column.FieldName, StringComparison.Ordinal));
                        if (string.IsNullOrEmpty(match.Field))
                            continue;

                        column.Width = Percent(match.Pixels, percentBasis);
                    }
                }
                finally
                {
                    grid.EndUpdate();
                }
            }
            catch (InvalidOperationException)
            {
            }
        }
        finally
        {
            applyingWidths--;
        }
    }

    private static IReadOnlyList<PersonListColumnShare.Width> BalanceRoster(
        IReadOnlyList<PersonListColumnShare.Width> widths,
        int available) =>
        PersonListColumnShare.Rebalance(
            widths,
            [(nameof(Person.Nationality), 56)],
            [
                (nameof(Person.EmployeeListResidenceAddress), Math.Max(240, available * 22 / 100)),
                (nameof(Person.EmployeeListDepartmentName), Math.Max(136, available * 11 / 100)),
                (nameof(Person.EmployeeListPositionName), Math.Max(180, available * 16 / 100)),
                (nameof(Person.PersonalNumber), Math.Max(120, available * 10 / 100)),
                (nameof(Person.EmployeeListEducationPlace), Math.Max(150, available * 12 / 100)),
                (nameof(Person.EmployeeListEducationProfession), Math.Max(140, available * 10 / 100)),
            ],
            PersonListColumnShare.FlexMin);

    private static int ColumnMinWidth(string? field)
    {
        if (FixedFields.Contains(field ?? string.Empty))
            return 36;
        if (string.Equals(field, nameof(Person.Nationality), StringComparison.Ordinal))
            return 48;
        return PersonListColumnShare.FlexMin;
    }

    private static string Percent(int pixels, int basis) =>
        (pixels * 100d / basis).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "%";

    private static void RenderCopyCells(RenderTreeBuilder builder, Person? person, string field, string ownerViewId)
    {
        IReadOnlyList<PersonListCopyCell> cells = person == null
            ? Array.Empty<PersonListCopyCell>()
            : field switch
            {
                nameof(Person.EmployeeListActivePassport) => new[] { PersonCurrentItems.ShownPassportCopy(person) },
                nameof(Person.EmployeeListEducationPlace) => PersonCurrentItems.ListShownEducationPlaces(person),
                nameof(Person.EmployeeListEducationProfession) => PersonCurrentItems.ListShownEducationProfessions(person),
                nameof(Person.EmployeeListResidenceAddress) => new[] { PersonCurrentItems.ShownAddressCopy(person) },
                nameof(Person.EmployeeListCvCopies) => PersonCurrentItems.ListCvCopies(person),
                nameof(Person.EmployeeListFamilyProofCopies) => PersonCurrentItems.ListFamilyProofCopies(person),
                _ => Array.Empty<PersonListCopyCell>(),
            };

        cells = cells.Where(cell => cell.Lines.Count > 0).ToList();
        var seq = 0;
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "person-doc-stack");
        if (cells.Count == 0)
        {
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "person-doc-line");
            builder.AddContent(seq++, "\u2014");
            builder.CloseElement();
            builder.CloseElement();
            return;
        }

        var personId = person!.ID;
        foreach (var cell in cells)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "person-doc-block");
            var link = personId != Guid.Empty && !string.IsNullOrEmpty(cell.RecordKey);
            for (var i = 0; i < cell.Lines.Count; i++)
            {
                if (i == 0 && link)
                {
                    builder.OpenElement(seq++, "button");
                    builder.AddAttribute(seq++, "type", "button");
                    builder.AddAttribute(seq++, "class", "person-doc-number");
                    builder.AddAttribute(seq++, "data-person-id", personId.ToString("D"));
                    builder.AddAttribute(seq++, "data-record-key", cell.RecordKey);
                    builder.AddAttribute(seq++, "data-display-name", cell.DisplayName ?? string.Empty);
                    builder.AddAttribute(seq++, "data-owner-view", ownerViewId);
                    if (!string.IsNullOrWhiteSpace(cell.DisplayName))
                        builder.AddAttribute(seq++, "title", cell.DisplayName);
                    builder.AddContent(seq++, cell.Lines[i]);
                    builder.CloseElement();
                }
                else
                {
                    builder.OpenElement(seq++, "span");
                    builder.AddAttribute(seq++, "class", "person-doc-line");
                    builder.AddContent(seq++, cell.Lines[i]);
                    builder.CloseElement();
                }
            }

            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private static void RenderDocuments(RenderTreeBuilder builder, Person? person, string field, string ownerViewId)
    {
        IReadOnlyList<PersonListActiveDocument> documents = person == null
            ? Array.Empty<PersonListActiveDocument>()
            : field switch
            {
                nameof(Person.EmployeeListActiveVisaNumbers) => PersonCurrentItems.ListActiveVisas(person),
                nameof(Person.EmployeeListActiveWorkPermitNumbers) => PersonCurrentItems.ListActiveWorkPermits(person),
                nameof(Person.EmployeeListActiveInvitationNumbers) => PersonCurrentItems.ListActiveInvitations(person),
                _ => Array.Empty<PersonListActiveDocument>(),
            };

        var seq = 0;
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "person-doc-stack");
        if (documents.Count == 0)
        {
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "person-doc-line");
            builder.AddContent(seq++, "\u2014");
            builder.CloseElement();
            builder.CloseElement();
            return;
        }

        var personId = person!.ID;
        foreach (var document in documents)
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "person-doc-block");
            var number = string.IsNullOrWhiteSpace(document.Number) ? "\u2014" : document.Number;
            if (personId != Guid.Empty && !string.IsNullOrEmpty(document.RecordKey))
            {
                builder.OpenElement(seq++, "button");
                builder.AddAttribute(seq++, "type", "button");
                builder.AddAttribute(seq++, "class", "person-doc-number");
                builder.AddAttribute(seq++, "data-person-id", personId.ToString("D"));
                builder.AddAttribute(seq++, "data-record-key", document.RecordKey);
                builder.AddAttribute(seq++, "data-display-name", document.DisplayName ?? string.Empty);
                builder.AddAttribute(seq++, "data-owner-view", ownerViewId);
                if (!string.IsNullOrWhiteSpace(document.DisplayName))
                    builder.AddAttribute(seq++, "title", document.DisplayName);
                builder.AddContent(seq++, number);
                builder.CloseElement();
            }
            else
            {
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "person-doc-line");
                builder.AddContent(seq++, number);
                builder.CloseElement();
            }

            if (!string.IsNullOrWhiteSpace(document.Detail))
            {
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "person-doc-line");
                builder.AddContent(seq++, document.Detail);
                builder.CloseElement();
            }

            if (!string.IsNullOrWhiteSpace(document.Dates))
            {
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "person-doc-line");
                builder.AddContent(seq++, document.Dates);
                builder.CloseElement();
            }

            builder.CloseElement();
        }

        builder.CloseElement();
    }

    private bool HideColumn(string propertyName)
    {
        var column = View?.Model?.Columns[propertyName];
        if (column == null || column.Index < 0)
            return false;

        column.Index = -1;
        return true;
    }

    private IJSRuntime? GetJs() => Application?.ServiceProvider?.GetService<IJSRuntime>();

    private async Task ReleaseJsAsync()
    {
        var js = GetJs();
        if (js == null)
            return;

        try
        {
            await js.InvokeVoidAsync("visaPersonRoster.release");
        }
        catch (JSDisconnectedException)
        {
        }
        catch (JSException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static string AppendCssClass(string? existing, string cssClass)
    {
        if (string.IsNullOrWhiteSpace(existing))
            return cssClass;
        if (existing.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(part => string.Equals(part, cssClass, StringComparison.Ordinal)))
            return existing;
        return existing + " " + cssClass;
    }

    private sealed class PersonListRosterMeasure
    {
        public int Available { get; set; }

        public int Viewport { get; set; }

        public int[] Preferred { get; set; } = Array.Empty<int>();
    }
}
