using System;
using System.Linq;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Model;

/// <summary>Hides deprecated <see cref="LookupBase.Name"/> on lookup detail views (except <see cref="ProjectContract"/>).</summary>
public sealed class LookupBaseDetailViewModelUpdater : ModelNodesGeneratorUpdater<ModelBOModelClassNodesGenerator>
{
    private const string NameMember = nameof(LookupBase.Name);
    private const string NameTmMember = nameof(LookupBase.NameTm);

    public override void UpdateNode(ModelNode node)
    {
        var boModel = (IModelBOModel)node;
        foreach (var lookupType in LookupBaseTypes())
        {
            if (lookupType == typeof(ProjectContract))
                continue;

            if (boModel[lookupType.FullName] is not IModelClass classModel)
                continue;

            HideMember(classModel, NameMember);
            EnsureNameTmVisible(classModel);
        }
    }

    private static void HideMember(IModelClass classModel, string memberName)
    {
        if (classModel.OwnMembers?[memberName] is IModelMember member)
        {
            member.AllowEdit = false;
            member.Index = -1;
        }
    }

    private static void EnsureNameTmVisible(IModelClass classModel)
    {
        if (classModel.OwnMembers?[NameTmMember] is IModelMember nameTm && (nameTm.Index ?? -1) < 0)
            nameTm.Index = 0;
    }

    private static Type[] LookupBaseTypes() =>
        typeof(LookupBase).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(LookupBase).IsAssignableFrom(t))
            .ToArray();
}
