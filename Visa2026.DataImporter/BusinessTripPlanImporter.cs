using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class BusinessTripPlanImporter
{
    private readonly ApiClient _api;
    private const string Entity = "BusinessTripPlan";

    public BusinessTripPlanImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<BusinessTripPlan>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var regionName = item.Region?.Name ?? "N/A";
            var cityName = item.City?.Name ?? "N/A";
            Console.WriteLine($"  [{item.Id}] {regionName}/{cityName} from {item.StartDate:d} to {item.EndDate:d}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<BusinessTripPlan?> CreateOneAsync(
        DateTime startDate,
        DateTime endDate,
        Guid regionId,
        Guid cityId)
    {
        Console.WriteLine($"=== POST {Entity} ===");

        var payload = new
        {
            StartDate = startDate,
            EndDate = endDate,
            Region = new { ID = regionId },
            City = new { ID = cityId }
        };

        try
        {
            var created = await _api.CreateAsync<BusinessTripPlan>(Entity, payload);
            Console.WriteLine($"  Created BusinessTripPlan ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating BusinessTripPlan: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<BusinessTripPlan> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    StartDate = record.StartDate,
                    EndDate = record.EndDate,
                    Region = record.Region != null ? new { ID = record.Region.Id } : null,
                    City = record.City != null ? new { ID = record.City.Id } : null
                };

                await _api.CreateAsync<BusinessTripPlan>(Entity, payload);
                var regionName = record.Region?.Name ?? "N/A";
                Console.WriteLine($"  ✓ Imported plan for: {regionName}");
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

    // ------------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------------
    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted BusinessTripPlan {id}\n");
    }
}