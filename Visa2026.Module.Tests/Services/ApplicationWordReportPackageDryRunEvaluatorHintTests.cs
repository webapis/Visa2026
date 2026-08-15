using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Dry-run hint paths that require a non-null <see cref="IObjectSpace"/> but must not query it
/// when <c>selectedItems</c> is supplied (distinct from null-OS early returns covered elsewhere).
/// </summary>
public class ApplicationWordReportPackageDryRunEvaluatorHintTests
{
    [Fact]
    public void CollectUserTemplateHints_EmptyPlaceholders_ReturnsEmptyWithoutQueryingObjectSpace()
    {
        var os = CreateNeverCalledObjectSpace();
        var application = new Application();
        var template = new UserReportTemplate();

        var hints = ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
            os,
            application,
            template,
            selectedItems: new List<ApplicationItem>());

        Assert.Empty(hints);
    }

    [Fact]
    public void CollectUserTemplateHints_ApplicationScalarEmpty_EmitsEmptyApplicationFieldHint()
    {
        var os = CreateNeverCalledObjectSpace();
        var application = new Application { ApplicationNumber = null };
        var template = new UserReportTemplate
        {
            RootBoType = UserReportBoType.Application,
            Placeholders = new ObservableCollection<UserReportPlaceholder>
            {
                new()
                {
                    IsValid = true,
                    PlaceholderKey = "ApplicationNumber",
                    ResolvedPropertyPath = "ApplicationNumber",
                },
            },
        };

        var hints = ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
            os,
            application,
            template,
            selectedItems: new List<ApplicationItem>());

        Assert.Contains(
            hints,
            h => h.MessageKey == "ApplicationReportPackage.Hint.EmptyApplicationField"
                 && h.FormatArgs is { Count: > 0 }
                 && string.Equals(h.FormatArgs[0]?.ToString(), "ApplicationNumber", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectUserTemplateHints_MissingPhotos_EmitsMissingPhotoHint()
    {
        var os = CreateNeverCalledObjectSpace();
        var application = new Application();
        var items = new List<ApplicationItem>
        {
            new() { ApplicationItemName = "Line A" },
            new() { ApplicationItemName = "Line B" },
        };
        var template = new UserReportTemplate
        {
            RootBoType = UserReportBoType.ApplicationItem,
            Placeholders = new ObservableCollection<UserReportPlaceholder>
            {
                new()
                {
                    IsValid = true,
                    PlaceholderKey = "IMAGE:Person_Photo",
                    ResolvedPropertyPath = "Person_Photo",
                },
            },
        };

        var hints = ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
            os,
            application,
            template,
            selectedItems: items);

        Assert.Contains(
            hints,
            h => h.MessageKey == "ApplicationReportPackage.Hint.MissingPhoto"
                 && h.FormatArgs is { Count: > 0 }
                 && h.FormatArgs[0]?.ToString() == "2");
    }

    [Fact]
    public void CollectUserTemplateHints_RowPlaceholderEmpty_EmitsEmptyItemFieldHint()
    {
        var os = CreateNeverCalledObjectSpace();
        var application = new Application();
        var items = new List<ApplicationItem>
        {
            new() { ApplicationItemName = "Item-1" },
        };

        var template = new UserReportTemplate
        {
            RootBoType = UserReportBoType.ApplicationItem,
            Placeholders = new ObservableCollection<UserReportPlaceholder>
            {
                new()
                {
                    IsValid = true,
                    PlaceholderKey = ".Person_FullName",
                    ResolvedPropertyPath = "Person_FullName",
                },
            },
        };

        var hints = ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
            os,
            application,
            template,
            selectedItems: items);

        Assert.Contains(
            hints,
            h => h.MessageKey == "ApplicationReportPackage.Hint.EmptyItemField"
                 && h.FormatArgs is { Count: 2 }
                 && string.Equals(h.FormatArgs[0]?.ToString(), "Item-1", StringComparison.Ordinal)
                 && string.Equals(h.FormatArgs[1]?.ToString(), "Person_FullName", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectUserTemplateHints_NeedsItemsButNoneSelected_ReturnsEmpty()
    {
        var os = CreateNeverCalledObjectSpace();
        var application = new Application();
        var template = new UserReportTemplate
        {
            RootBoType = UserReportBoType.ApplicationItem,
            Placeholders = new ObservableCollection<UserReportPlaceholder>
            {
                new()
                {
                    IsValid = true,
                    PlaceholderKey = ".Person_FullName",
                    ResolvedPropertyPath = "Person_FullName",
                },
            },
        };

        var hints = ApplicationWordReportPackageDryRunEvaluator.CollectUserTemplateHints(
            os,
            application,
            template,
            selectedItems: new List<ApplicationItem>());

        Assert.Empty(hints);
    }

    private static IObjectSpace CreateNeverCalledObjectSpace() =>
        DispatchProxy.Create<IObjectSpace, NeverCalledObjectSpaceProxy>();

    private class NeverCalledObjectSpaceProxy : DispatchProxy
    {
        protected override object Invoke(MethodInfo targetMethod, object[] args) =>
            throw new InvalidOperationException(
                $"Unexpected IObjectSpace call: {targetMethod.Name}");
    }
}
