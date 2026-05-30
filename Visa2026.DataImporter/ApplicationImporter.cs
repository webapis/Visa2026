using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class ApplicationImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Application";

    public ApplicationImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<Application>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var typeName = item.ApplicationType?.Name ?? "Unknown Type";
            Console.WriteLine($"  [{item.Id}] {item.ApplicationNumber} ({item.Year}) - {typeName}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Application?> CreateOneAsync(
        DateTime appDate,
        ApplicationTypeCategory category,
        Guid applicationTypeId,
        Guid? applicationTypeFilterId = null, // Legacy; no longer sent to API
        Guid? projectContractId = null,
        Guid? visaPeriodId = null,
        Guid? visaCategoryId = null,
        Guid? visaTypeId = null,
        Guid? migrationServiceId = null,
        Guid? urgencyId = null,
        Guid? fromCityId = null,
        Guid? toCityId = null)
    {
        Console.WriteLine($"=== POST {Entity} ===");

        // Construct payload with required relationships
        var payload = new
        {
            ApplicationDate = appDate,

            // Required Lookups
            ApplicationType = new { ID = applicationTypeId },

            // Optional / Conditional Lookups
            ProjectContract = projectContractId.HasValue ? new { ID = projectContractId.Value } : null,
            VisaPeriod = visaPeriodId.HasValue ? new { ID = visaPeriodId.Value } : null,
            VisaCategory = visaCategoryId.HasValue ? new { ID = visaCategoryId.Value } : null,
            VisaType = visaTypeId.HasValue ? new { ID = visaTypeId.Value } : null,
            MigrationService = migrationServiceId.HasValue ? new { ID = migrationServiceId.Value } : null,
            Urgency = urgencyId.HasValue ? new { ID = urgencyId.Value } : null,
            FromCity = fromCityId.HasValue ? new { ID = fromCityId.Value } : null,
            ToCity = toCityId.HasValue ? new { ID = toCityId.Value } : null,
        };

        try
        {
            var created = await _api.CreateAsync<Application>(Entity, payload);
            Console.WriteLine($"  Created Application: {created?.ApplicationNumber} (ID: {created?.Id})");
            Console.WriteLine();
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating application: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Application> records, Guid defaultFilterId)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                // For bulk import, we assume some IDs might come from the record's objects 
                // or fall back to defaults provided as arguments if null in the source.
                var payload = new
                {
                    ApplicationDate = record.ApplicationDate,
                    ApplicationNumber = record.ApplicationNumber, // Include ApplicationNumber from Excel
                    AppNumberPrefix = record.AppNumberPrefix,     // Include AppNumberPrefix from Excel
                    Year = record.Year,                           // Include Year from Excel

                    ApplicationType = record.ApplicationType != null ? new { ID = record.ApplicationType.Id } : null,

                    ProjectContract = record.ProjectContract != null ? new { ID = record.ProjectContract.Id } : null,
                    VisaCategory = record.VisaCategory != null ? new { ID = record.VisaCategory.Id } : null,
                    MigrationService = record.MigrationService != null ? new { ID = record.MigrationService.Id } : null,
                    Urgency = record.Urgency != null ? new { ID = record.Urgency.Id } : null,
                    VisaPeriod = record.VisaPeriod != null ? new { ID = record.VisaPeriod.Id } : null,
                    VisaType = record.VisaType != null ? new { ID = record.VisaType.Id } : null,
                    FromCity = record.FromCity != null ? new { ID = record.FromCity.Id } : null,
                    ToCity = record.ToCity != null ? new { ID = record.ToCity.Id } : null,
                    MovementPermitLocation = record.MovementPermitLocation != null ? new { ID = record.MovementPermitLocation.Id } : null,
                    BorderZoneLocation = record.BorderZoneLocation != null ? new { ID = record.BorderZoneLocation.Id } : null,
                };

                await _api.CreateAsync<Application>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Application");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import: {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    // Exposed for testing
    public static Guid ResolveFilterId(Application record, Guid defaultFilterId)
    {
        // 1. Explicit on record
        if (record.ApplicationTypeFilter != null) return record.ApplicationTypeFilter.Id;
        // 2. Inferred from ApplicationType
        if (record.ApplicationType?.ApplicationTypeFilter != null) return record.ApplicationType.ApplicationTypeFilter.Id;
        // 3. Fallback to default
        return defaultFilterId;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted Application {id}\n");
    }
}