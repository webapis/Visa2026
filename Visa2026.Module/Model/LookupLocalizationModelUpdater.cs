using System;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Model;

/// <summary>
/// Layer B: global lookup pickers use <see cref="LookupBase.LocalizedDisplayName"/> in combos and lookup ListViews.
/// </summary>
public sealed class LookupLocalizationModelUpdater : ModelNodesGeneratorUpdater<ModelBOModelClassNodesGenerator>
{
    private const string LocalizedDisplayMember = nameof(LookupBase.LocalizedDisplayName);
    private const string NameTmMember = nameof(LookupBase.NameTm);

    public override void UpdateNode(ModelNode node)
    {
        var boModel = (IModelBOModel)node;
        foreach (var lookupType in LocalizedLookupTypes.All)
        {
            ConfigureLookupProperty(boModel, lookupType, LocalizedDisplayMember);
        }

        ConfigureLookupProperty(boModel, typeof(Subcontractor), NameTmMember);
    }

    private static void ConfigureLookupProperty(IModelBOModel boModel, Type lookupType, string lookupProperty)
    {
        foreach (IModelClass referencingClass in boModel)
        {
            if (referencingClass.OwnMembers == null)
                continue;

            foreach (IModelMember member in referencingClass.OwnMembers)
            {
                if (member.Type != lookupType)
                    continue;

                member.LookupProperty = lookupProperty;
            }
        }
    }
}

/// <summary>
/// Lookup popups for global catalogs show a single culture-aware column (<see cref="LookupBase.LocalizedDisplayName"/>).
/// </summary>
public sealed class LookupLocalizationLookupListViewUpdater : ModelNodesGeneratorUpdater<ModelViewsNodesGenerator>
{
    private const string LocalizedDisplayMember = nameof(LookupBase.LocalizedDisplayName);
    private const string NameTmMember = nameof(LookupBase.NameTm);

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var lookupType in LocalizedLookupTypes.All)
        {
            ConfigureLookupListView(views, lookupType, LocalizedDisplayMember);
        }

        ConfigureLookupListView(views, typeof(Subcontractor), NameTmMember);
    }

    private static void ConfigureLookupListView(IModelViews views, Type lookupType, string displayProperty)
    {
        var viewId = lookupType.Name + "_LookupListView";
        if (views[viewId] is IModelListView lookupListView)
        {
            ConfigureSingleColumnLookupListView(lookupListView, displayProperty);
        }
    }

    private static void ConfigureSingleColumnLookupListView(IModelListView lookupListView, string displayProperty)
    {
        foreach (var column in lookupListView.Columns.ToList())
        {
            if (column.PropertyName == displayProperty)
                continue;

            column.Index = -1;
        }

        var displayColumn = lookupListView.Columns[displayProperty]
            ?? lookupListView.Columns.AddNode<IModelColumn>(displayProperty);
        displayColumn.PropertyName = displayProperty;
        displayColumn.Index = 0;
    }
}
