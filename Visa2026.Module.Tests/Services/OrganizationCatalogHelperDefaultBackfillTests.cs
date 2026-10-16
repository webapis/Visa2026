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
/// EnsureOneDefault / AssignDefaultsIfEmpty / BackfillUnassigned for organization letterhead catalogs.
/// Found-id assign and make-default live in open coverage PRs; this file covers default repair + backfill only.
/// </summary>
public class OrganizationCatalogHelperDefaultBackfillTests
{
    [Fact]
    public void EnsureOneDefault_empty_catalog_returns_zero()
    {
        var os = CatalogObjectSpaceStub.Create();

        Assert.Equal(0, OrganizationCatalogHelper.EnsureOneDefault(os));
    }

    [Fact]
    public void EnsureOneDefault_already_one_default_returns_zero()
    {
        var company = new CompanyProfile { ID = Guid.NewGuid(), Name = "Alpha", IsDefault = true };
        var os = CatalogObjectSpaceStub.Create(company);

        Assert.Equal(0, OrganizationCatalogHelper.EnsureOneDefault(os));
        Assert.True(company.IsDefault);
    }

    [Fact]
    public void EnsureOneDefault_no_default_sets_first_alphabetically()
    {
        var beta = new CompanyProfile { ID = Guid.NewGuid(), Name = "Beta", IsDefault = false };
        var alpha = new CompanyProfile { ID = Guid.NewGuid(), Name = "Alpha", IsDefault = false };
        var os = CatalogObjectSpaceStub.Create(beta, alpha);

        Assert.Equal(1, OrganizationCatalogHelper.EnsureOneDefault(os));
        Assert.True(alpha.IsDefault);
        Assert.False(beta.IsDefault);
    }

    [Fact]
    public void EnsureOneDefault_multiple_defaults_keeps_alphabetical_first()
    {
        var beta = new CompanyProfile { ID = Guid.NewGuid(), Name = "Beta", IsDefault = true };
        var alpha = new CompanyProfile { ID = Guid.NewGuid(), Name = "Alpha", IsDefault = true };
        var gamma = new CompanyProfile { ID = Guid.NewGuid(), Name = "Gamma", IsDefault = true };
        var os = CatalogObjectSpaceStub.Create(beta, alpha, gamma);

        // Two extras cleared (Alpha kept).
        Assert.Equal(2, OrganizationCatalogHelper.EnsureOneDefault(os));
        Assert.True(alpha.IsDefault);
        Assert.False(beta.IsDefault);
        Assert.False(gamma.IsDefault);
    }

    [Fact]
    public void AssignDefaultsIfEmpty_fills_null_slots_from_catalog_defaults()
    {
        var company = new CompanyProfile { ID = Guid.NewGuid(), Name = "Çalık", IsDefault = true };
        var signatory = new AuthorizedSignatory { ID = Guid.NewGuid(), FullName = "Serdar", IsDefault = true };
        var representative = new AuthorizedRepresentative
        {
            ID = Guid.NewGuid(),
            FullName = "Ali",
            IsDefault = true,
        };
        var os = CatalogObjectSpaceStub.Create(company, signatory, representative);
        var app = new ApplicationProfileInstance();

        OrganizationCatalogHelper.AssignDefaultsIfEmpty(app, os);

        Assert.Same(company, app.OrganizationCompany);
        Assert.Same(signatory, app.OrganizationSignatory);
        Assert.Same(representative, app.OrganizationRepresentative);
    }

    [Fact]
    public void AssignDefaultsIfEmpty_does_not_overwrite_existing_relations()
    {
        var kept = new CompanyProfile { ID = Guid.NewGuid(), Name = "Kept" };
        var catalogDefault = new CompanyProfile { ID = Guid.NewGuid(), Name = "Default", IsDefault = true };
        var os = CatalogObjectSpaceStub.Create(catalogDefault);
        var app = new ApplicationProfileInstance { OrganizationCompany = kept };

        OrganizationCatalogHelper.AssignDefaultsIfEmpty(app, os);

        Assert.Same(kept, app.OrganizationCompany);
    }

    [Fact]
    public void BackfillUnassigned_counts_instances_that_gained_defaults()
    {
        var company = new CompanyProfile { ID = Guid.NewGuid(), Name = "Çalık", IsDefault = true };
        var alreadySet = new ApplicationProfileInstance
        {
            OrganizationCompany = new CompanyProfile { ID = Guid.NewGuid(), Name = "Other" },
        };
        var needingFill = new ApplicationProfileInstance();
        var os = CatalogObjectSpaceStub.Create(company, alreadySet, needingFill);

        Assert.Equal(1, OrganizationCatalogHelper.BackfillUnassigned(os));
        Assert.Same(company, needingFill.OrganizationCompany);
        Assert.Equal("Other", alreadySet.OrganizationCompany!.Name);
    }

    /// <summary>
    /// DispatchProxy stub: GetObjectsQuery for catalog + application rows.
    /// Must not be sealed (DispatchProxy requirement).
    /// </summary>
    private class CatalogObjectSpaceStub : DispatchProxy
    {
        private readonly List<BaseObject> _all = new();

        public static IObjectSpace Create(params BaseObject[] entities)
        {
            var proxy = Create<IObjectSpace, CatalogObjectSpaceStub>();
            var stub = (CatalogObjectSpaceStub)proxy;
            foreach (var entity in entities)
                stub._all.Add(entity);
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IObjectSpace.GetObjectsQuery))
            {
                var elementType = targetMethod.ReturnType.GetGenericArguments()[0];
                var matches = _all.Where(x => elementType.IsInstanceOfType(x)).ToList();
                return typeof(CatalogObjectSpaceStub)
                    .GetMethod(nameof(AsQueryableOf), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(elementType)
                    .Invoke(null, [matches]);
            }

            throw new InvalidOperationException(
                $"Unexpected IObjectSpace call: {targetMethod.Name}");
        }

        private static IQueryable<T> AsQueryableOf<T>(IList matches) =>
            matches.Cast<T>().AsQueryable();
    }
}
