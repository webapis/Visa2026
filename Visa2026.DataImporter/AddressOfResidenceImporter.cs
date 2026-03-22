using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class AddressOfResidenceImporter
{
    private readonly ApiClient _api;
    private const string Entity = "AddressOfResidence";

    public AddressOfResidenceImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<AddressOfResidence>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var personName = item.Person?.FullName ?? "Unknown Person";
            Console.WriteLine($"  [{item.Id}] {personName} - {item.FullAddress} ({item.Type})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<AddressOfResidence?> CreateOneAsync(
        Guid personId,
        ResidenceType type,
        string fullAddress,
        Guid regionId,
        Guid cityId,
        DateTime startDate,
        DateTime expirationDate,
        Guid? lodgingId = null)
    {
        Console.WriteLine($"=== POST {Entity} for Person ID: {personId} ===");

        var payload = new
        {
            Person = new { ID = personId },
            Type = type,
            FullAddress = fullAddress,
            Region = new { ID = regionId },
            City = new { ID = cityId },
            StartDate = startDate,
            ExpirationDate = expirationDate,
            Lodging = (type == ResidenceType.Lodging && lodgingId.HasValue) ? new { ID = lodgingId.Value } : null
        };

        try
        {
            var created = await _api.CreateAsync<AddressOfResidence>(Entity, payload);
            Console.WriteLine($"  Created AddressOfResidence ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating AddressOfResidence: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<AddressOfResidence> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    Type = record.Type,
                    FullAddress = record.FullAddress,
                    Region = record.Region != null ? new { ID = record.Region.Id } : null,
                    City = record.City != null ? new { ID = record.City.Id } : null,
                    StartDate = record.StartDate,
                    ExpirationDate = record.ExpirationDate,
                    Lodging = record.Lodging != null ? new { ID = record.Lodging.Id } : null
                };

                await _api.CreateAsync<AddressOfResidence>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Address for: {record.Person?.FullName ?? "Unknown"}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import for '{record.Person?.FullName ?? "Unknown"}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted AddressOfResidence {id}\n");
    }
}