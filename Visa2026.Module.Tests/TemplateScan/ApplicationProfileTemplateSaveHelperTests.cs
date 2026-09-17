#nullable enable

using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

public class ApplicationProfileTemplateSaveHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Save_rejects_blank_template_name(string? name)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            ApplicationProfileTemplateSaveHelper.Save(new ApplicationProfileTemplateSaveRequest
            {
                ObjectSpace = null!,
                Profile = null!,
                TemplateName = name!,
                DataScope = default,
                CatalogScope = default,
                Content = [1],
            }));

        Assert.Contains("template name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyCatalogApplicability_SharedCatalog_ClearsContractBinding()
    {
        var contractId = Guid.NewGuid();
        var template = new ApplicationProfileTemplate
        {
            CatalogScope = ApplicationProfileTemplateCatalogScope.Global,
            ApplicableProjectContractId = contractId,
        };

        ApplicationProfileTemplateSaveHelper.ApplyCatalogApplicability(
            template,
            objectSpace: null,
            ApplicationProfileTemplateCatalogScope.Global,
            contractId,
            migrationServiceId: null);

        Assert.Null(template.ApplicableProjectContractId);
        Assert.Null(template.ApplicableMigrationServiceId);
    }

    [Fact]
    public void ApplyCatalogApplicability_ProfileSpecific_SetsProjectContract()
    {
        var contractId = Guid.NewGuid();
        var template = new ApplicationProfileTemplate
        {
            CatalogScope = ApplicationProfileTemplateCatalogScope.ProfileSpecific,
        };

        ApplicationProfileTemplateSaveHelper.ApplyCatalogApplicability(
            template,
            objectSpace: null,
            ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            contractId,
            migrationServiceId: null);

        Assert.Equal(contractId, template.ApplicableProjectContractId);
        Assert.Null(template.ApplicableMigrationServiceId);
    }

    [Fact]
    public void ApplyCatalogApplicability_ProfileSpecificEmpty_MeansAllInstances()
    {
        var template = new ApplicationProfileTemplate
        {
            CatalogScope = ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            ApplicableProjectContractId = Guid.NewGuid(),
            ApplicableMigrationServiceId = Guid.NewGuid(),
        };

        ApplicationProfileTemplateSaveHelper.ApplyCatalogApplicability(
            template,
            objectSpace: null,
            ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            projectContractId: null,
            migrationServiceId: null);

        Assert.Null(template.ApplicableProjectContractId);
        Assert.Null(template.ApplicableMigrationServiceId);
    }

    [Fact]
    public void SaveRequest_omits_source_so_convert_does_not_require_yellow_bytes()
    {
        var request = new ApplicationProfileTemplateSaveRequest
        {
            ObjectSpace = null!,
            Profile = null!,
            TemplateName = "Converted",
            DataScope = ApplicationProfileTemplateDataScope.Both,
            CatalogScope = ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            Content = [1],
        };

        Assert.Null(request.SourceContent);
        Assert.Null(request.SourceFileName);
    }

    [Fact]
    public void ShouldWriteLinkedMasterFile_ThisProfile_SkipsExistingSharedName()
    {
        Assert.True(ApplicationProfileTemplateSaveHelper.ShouldWriteLinkedMasterFile(
            ApplicationProfileTemplateCatalogScope.Global,
            nameIsUsedAsSharedInclude: true));
        Assert.True(ApplicationProfileTemplateSaveHelper.ShouldWriteLinkedMasterFile(
            ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            nameIsUsedAsSharedInclude: false));
        Assert.False(ApplicationProfileTemplateSaveHelper.ShouldWriteLinkedMasterFile(
            ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            nameIsUsedAsSharedInclude: true));
    }

    [Fact]
    public void AllocateUniqueTemplateName_WordRosterNameTaken_Appends_2()
    {
        var unique = ApplicationProfileTemplateSaveHelper.AllocateUniqueTemplateName(
            "Dasary_yurt_rayatlarynyn_sanawy_cakylyk",
            ["DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK", "YUZTUTMA"]);

        Assert.Equal("Dasary_yurt_rayatlarynyn_sanawy_cakylyk_2", unique);
    }

    [Fact]
    public void AllocateUniqueTemplateName_SecondCopy_Appends_3()
    {
        var unique = ApplicationProfileTemplateSaveHelper.AllocateUniqueTemplateName(
            "Sanaw",
            ["Sanaw", "Sanaw_2"]);

        Assert.Equal("Sanaw_3", unique);
    }

    [Fact]
    public void AllocateUniqueTemplateName_FreeName_Unchanged()
    {
        Assert.Equal(
            "New_sanaw",
            ApplicationProfileTemplateSaveHelper.AllocateUniqueTemplateName(
                "New_sanaw",
                ["Other"]));
    }
}
