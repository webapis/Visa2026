#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Found-id / missing-id assign and make-default success for organization letterhead catalogs.
/// Unknown-kind / empty-id gates live in open PR #66.
/// </summary>
public class OrganizationCatalogHelperFoundIdAssignTests
{
    [Fact]
    public void TryAssign_company_found_id_sets_OrganizationCompany()
    {
        var companyId = Guid.NewGuid();
        var company = new CompanyProfile { ID = companyId, Name = "Çalık Enerji" };
        var os = CatalogObjectSpaceStub.Create(company);
        var app = new ApplicationProfileInstance();

        Assert.True(OrganizationCatalogHelper.TryAssign(
            app, os, OrganizationCatalogHelper.Company, companyId, out var error));
        Assert.Null(error);
        Assert.Same(company, app.OrganizationCompany);
    }

    [Fact]
    public void TryAssign_company_missing_id_returns_not_found()
    {
        var os = CatalogObjectSpaceStub.Create();
        var app = new ApplicationProfileInstance();

        Assert.False(OrganizationCatalogHelper.TryAssign(
            app, os, OrganizationCatalogHelper.Company, Guid.NewGuid(), out var error));
        Assert.Equal("Company not found.", error);
        Assert.Null(app.OrganizationCompany);
    }

    [Fact]
    public void TryAssign_signatory_found_id_sets_OrganizationSignatory()
    {
        var signatoryId = Guid.NewGuid();
        var signatory = new AuthorizedSignatory { ID = signatoryId, FullName = "Serdar" };
        var os = CatalogObjectSpaceStub.Create(signatory);
        var app = new ApplicationProfileInstance();

        Assert.True(OrganizationCatalogHelper.TryAssign(
            app, os, OrganizationCatalogHelper.Signatory, signatoryId, out var error));
        Assert.Null(error);
        Assert.Same(signatory, app.OrganizationSignatory);
    }

    [Fact]
    public void TryMakeDefault_missing_record_returns_not_found()
    {
        var os = CatalogObjectSpaceStub.Create();

        Assert.False(OrganizationCatalogHelper.TryMakeDefault(
            os, OrganizationCatalogHelper.Company, Guid.NewGuid(), out var error));
        Assert.Equal("Catalog record not found.", error);
    }

    [Fact]
    public void TryMakeDefault_company_clears_other_defaults_and_commits()
    {
        var keeperId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var keeper = new CompanyProfile { ID = keeperId, Name = "Alpha", IsDefault = false };
        var other = new CompanyProfile { ID = otherId, Name = "Beta", IsDefault = true };
        var os = CatalogObjectSpaceStub.Create(keeper, other);
        var stub = (CatalogObjectSpaceStub)os;

        Assert.True(OrganizationCatalogHelper.TryMakeDefault(
            os, OrganizationCatalogHelper.Company, keeperId, out var error));
        Assert.Null(error);
        Assert.True(keeper.IsDefault);
        Assert.False(other.IsDefault);
        Assert.True(stub.CommitCalled);
    }

    /// <summary>
    /// DispatchProxy stub: GetObjectByKey / GetObjectsQuery / SetModified / CommitChanges
    /// for organization catalog assign + make-default.
    /// </summary>
    private class CatalogObjectSpaceStub : DispatchProxy
    {
        private readonly Dictionary<Guid, BaseObject> _byId = new();
        private readonly List<BaseObject> _all = new();
        private bool _modified;

        public bool CommitCalled { get; private set; }

        public static IObjectSpace Create(params BaseObject[] entities)
        {
            var proxy = Create<IObjectSpace, CatalogObjectSpaceStub>();
            var stub = (CatalogObjectSpaceStub)proxy;
            foreach (var entity in entities)
            {
                stub._all.Add(entity);
                if (entity.ID != Guid.Empty)
                    stub._byId[entity.ID] = entity;
            }

            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IObjectSpace.GetObjectByKey)
                && args is { Length: 1 }
                && args[0] is Guid id)
            {
                return _byId.TryGetValue(id, out var entity) ? entity : null;
            }

            if (targetMethod.Name == nameof(IObjectSpace.GetObjectsQuery))
            {
                var elementType = targetMethod.ReturnType.GetGenericArguments()[0];
                var matches = _all.Where(x => elementType.IsInstanceOfType(x)).ToList();
                return typeof(CatalogObjectSpaceStub)
                    .GetMethod(nameof(AsQueryableOf), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(elementType)
                    .Invoke(null, [matches]);
            }

            if (targetMethod.Name == nameof(IObjectSpace.SetModified))
            {
                _modified = true;
                return null;
            }

            if (targetMethod.Name == "get_IsModified")
                return _modified;

            if (targetMethod.Name == nameof(IObjectSpace.CommitChanges))
            {
                CommitCalled = true;
                _modified = false;
                return null;
            }

            throw new InvalidOperationException(
                $"Unexpected IObjectSpace call: {targetMethod.Name}");
        }

        private static IQueryable<T> AsQueryableOf<T>(IList matches) =>
            matches.Cast<T>().AsQueryable();
    }
}
