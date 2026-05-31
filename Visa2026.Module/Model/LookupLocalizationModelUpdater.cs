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

    public override void UpdateNode(ModelNode node)
    {
        var boModel = (IModelBOModel)node;
        foreach (var lookupType in LocalizedLookupTypes.All)
        {
            foreach (IModelClass referencingClass in boModel)
            {
                if (referencingClass.OwnMembers == null)
                    continue;

                foreach (IModelMember member in referencingClass.OwnMembers)
                {
                    if (member.Type != lookupType)
                        continue;

                    member.LookupProperty = LocalizedDisplayMember;
                }
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

    public override void UpdateNode(ModelNode node)
    {
        var views = (IModelViews)node;
        foreach (var lookupType in LocalizedLookupTypes.All)
        {
            var viewId = lookupType.Name + "_LookupListView";
            if (views[viewId] is IModelListView lookupListView)
            {
                ConfigureSingleColumnLookupListView(lookupListView);
            }
        }
    }

    private static void ConfigureSingleColumnLookupListView(IModelListView lookupListView)
    {
        foreach (var column in lookupListView.Columns.ToList())
        {
            if (column.PropertyName == LocalizedDisplayMember)
                continue;

            column.Index = -1;
        }

        var displayColumn = lookupListView.Columns[LocalizedDisplayMember]
            ?? lookupListView.Columns.AddNode<IModelColumn>(LocalizedDisplayMember);
        displayColumn.PropertyName = LocalizedDisplayMember;
        displayColumn.Index = 0;
    }
}
