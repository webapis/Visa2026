using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class BusinessTripAddressImporter
{
    private readonly ApiClient _api;
    private const string Entity = "BusinessTripAddress";

    public BusinessTripAddressImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}es ===");
        var items = await _api.GetAllAsync<BusinessTripAddress>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var cityName = item.City?.Name ?? "Unknown City";
            Console.WriteLine($"  [{item.Id}] {item.FullAddress} ({cityName})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<BusinessTripAddress?> CreateOneAsync(
        Guid cityId,
        string fullAddress)
    {
        Console.WriteLine($"=== POST {Entity} ===");

        var payload = new
        {
            City = new { ID = cityId },
            FullAddress = fullAddress
        };

        try
        {
            var created = await _api.CreateAsync<BusinessTripAddress>(Entity, payload);
            Console.WriteLine($"  Created BusinessTripAddress ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating BusinessTripAddress: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<BusinessTripAddress> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}es ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    City = record.City != null ? new { ID = record.City.Id } : null,
                    FullAddress = record.FullAddress
                };

                await _api.CreateAsync<BusinessTripAddress>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Address: {record.FullAddress}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import for '{record.FullAddress}': {ex.Message}");
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
        Console.WriteLine($"  Deleted BusinessTripAddress {id}\n");
    }
}