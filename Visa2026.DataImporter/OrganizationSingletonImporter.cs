using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

/// <summary>
/// OData upsert helpers for organization singleton BOs (Phase 4 import tooling).
/// </summary>
public class OrganizationSingletonImporter
{
    private readonly ApiClient _api;

    public OrganizationSingletonImporter(ApiClient api) => _api = api;

    public async Task<CompanyProfileDto?> UpsertCompanyProfileAsync(object payload)
    {
        return await UpsertSingletonAsync<CompanyProfileDto>("CompanyProfile", payload);
    }

    public async Task<ApplicationNumberingProfileDto?> UpsertApplicationNumberingProfileAsync(object payload)
    {
        return await UpsertSingletonAsync<ApplicationNumberingProfileDto>("ApplicationNumberingProfile", payload);
    }

    public async Task<AuthorizedSignatoryDto?> UpsertAuthorizedSignatoryAsync(object payload)
    {
        return await UpsertSingletonAsync<AuthorizedSignatoryDto>("AuthorizedSignatory", payload);
    }

    public async Task<AuthorizedRepresentativeDto?> UpsertAuthorizedRepresentativeAsync(object payload)
    {
        return await UpsertSingletonAsync<AuthorizedRepresentativeDto>("AuthorizedRepresentative", payload);
    }

    private async Task<T?> UpsertSingletonAsync<T>(string entityName, object payload) where T : class
    {
        var existing = (await _api.QueryAsync<IdHolder>(entityName, "$top=1")).FirstOrDefault();
        if (existing != null && existing.Id != Guid.Empty)
        {
            await _api.UpdateAsync(entityName, existing.Id, payload);
            return await _api.GetByIdAsync<T>(entityName, existing.Id);
        }

        return await _api.CreateAsync<T>(entityName, payload);
    }

    private sealed class IdHolder
    {
        [System.Text.Json.Serialization.JsonPropertyName("ID")]
        public Guid Id { get; set; }
    }
}
