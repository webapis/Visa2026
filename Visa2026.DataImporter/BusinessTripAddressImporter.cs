using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class BusinessTripAddressImporter : BaseImporter<BusinessTripAddress>
{
    private const string Entity = "BusinessTripAddress";

    public BusinessTripAddressImporter(ApiClient api) : base(api, Entity)
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {EntityName}es ===");
        var items = await Api.GetAllAsync<BusinessTripAddress>(EntityName);
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
        Console.WriteLine($"=== POST {EntityName} ===");

        var payload = new
    {
            City = new { ID = cityId },
            FullAddress = fullAddress
        };

        try
        {
            var created = await Api.CreateAsync<BusinessTripAddress>(EntityName, payload);
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
        Console.WriteLine($"=== Bulk import {EntityName}es ===");
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

        await Api.CreateAsync<BusinessTripAddress>(EntityName, payload);
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
        await Api.DeleteAsync(EntityName, id);
        Console.WriteLine($"  Deleted BusinessTripAddress {id}\n");
}
}