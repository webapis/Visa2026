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
            var compName = item.Company?.Name ?? "Unknown Company";
            Console.WriteLine($"  [{item.Id}] {item.ApplicationNumber} ({item.Year}) - {typeName} [{compName}]");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Application?> CreateOneAsync(
        DateTime appDate,
        ApplicationTypeCategory category,
        Guid companyId,
        Guid companyHeadId,
        Guid representativeId,
        Guid applicationTypeId,
        Guid applicationTypeFilterId, // Required by server logic
        Guid? projectContractId = null,
        Guid? visaPeriodId = null,
        Guid? urgencyId = null)
    {
        Console.WriteLine($"=== POST {Entity} ===");

        // Construct payload with required relationships
        var payload = new
        {
            ApplicationDate = appDate,
            Category = category,
            
            // Required Lookups
            Company = new { ID = companyId },
            CompanyHead = new { ID = companyHeadId },
            Representative = new { ID = representativeId },
            ApplicationType = new { ID = applicationTypeId },
            ApplicationTypeFilter = new { ID = applicationTypeFilterId },

            // Optional / Conditional Lookups
            ProjectContract = projectContractId.HasValue ? new { ID = projectContractId.Value } : null,
            VisaPeriod = visaPeriodId.HasValue ? new { ID = visaPeriodId.Value } : null,
            Urgency = urgencyId.HasValue ? new { ID = urgencyId.Value } : null,

            IsActive = true
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
    public async Task BulkImportAsync(IEnumerable<Application> records, 
        Guid defaultCompanyId, Guid defaultHeadId, Guid defaultRepId, Guid defaultFilterId)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                Guid filterId = ResolveFilterId(record, defaultFilterId);

                // For bulk import, we assume some IDs might come from the record's objects 
                // or fall back to defaults provided as arguments if null in the source.
                var payload = new
                {
                    ApplicationDate = record.ApplicationDate,
                    Category = record.Category,
                    
                    // Map relationships: prefer record's own property, else fallback
                    Company = record.Company != null ? new { ID = record.Company.Id } : new { ID = defaultCompanyId },
                    ApplicationType = record.ApplicationType != null ? new { ID = record.ApplicationType.Id } : null,
                    
                    // These are often not populated in simple import models, so we might use defaults
                    CompanyHead = new { ID = defaultHeadId },
                    Representative = new { ID = defaultRepId },
                    ApplicationTypeFilter = new { ID = filterId },

                    IsActive = true
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