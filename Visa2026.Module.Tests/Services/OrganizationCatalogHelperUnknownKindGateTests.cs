#nullable enable

using System;
using System.Reflection;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Fail-closed gates for organization letterhead assign / make-default
/// (unknown kind, empty id, blank kind) without loading catalog rows.
/// </summary>
public class OrganizationCatalogHelperUnknownKindGateTests
{
    [Fact]
    public void TryMakeDefault_rejects_empty_id_without_catalog_lookup()
    {
        var os = ThrowingGetObjectObjectSpaceStub.Create();

        Assert.False(OrganizationCatalogHelper.TryMakeDefault(
            os, OrganizationCatalogHelper.Company, Guid.Empty, out var error));
        Assert.Equal("Select a catalog record first.", error);
    }

    [Fact]
    public void TryMakeDefault_rejects_unknown_kind()
    {
        var os = ThrowingGetObjectObjectSpaceStub.Create();

        Assert.False(OrganizationCatalogHelper.TryMakeDefault(
            os, "not-a-kind", Guid.NewGuid(), out var error));
        Assert.Equal("Unknown organization catalog.", error);
    }

    [Fact]
    public void TryAssign_rejects_blank_and_unknown_kind()
    {
        var os = ThrowingGetObjectObjectSpaceStub.Create();
        var app = new ApplicationProfileInstance();

        Assert.False(OrganizationCatalogHelper.TryAssign(app, os, "  ", Guid.NewGuid(), out var blankError));
        Assert.Equal("Missing organization field.", blankError);

        Assert.False(OrganizationCatalogHelper.TryAssign(app, os, "payroll", Guid.NewGuid(), out var unknownError));
        Assert.Equal("Unknown organization field.", unknownError);
    }

    [Fact]
    public void TryAssign_clears_company_when_id_null_without_lookup()
    {
        var os = ThrowingGetObjectObjectSpaceStub.Create();
        var app = new ApplicationProfileInstance
        {
            OrganizationCompany = new CompanyProfile { Name = "Was set" },
        };

        Assert.True(OrganizationCatalogHelper.TryAssign(
            app, os, OrganizationCatalogHelper.Company, id: null, out var error));
        Assert.Null(error);
        Assert.Null(app.OrganizationCompany);
    }

    private class ThrowingGetObjectObjectSpaceStub : DispatchProxy
    {
        public static IObjectSpace Create() =>
            Create<IObjectSpace, ThrowingGetObjectObjectSpaceStub>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException(
                $"Unexpected IObjectSpace call: {targetMethod?.Name}");
    }
}
