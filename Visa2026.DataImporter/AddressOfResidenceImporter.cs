using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class AddressOfResidenceImporter : BaseImporter<AddressOfResidence>
{
    private const string Entity = "AddressOfResidence";

    public AddressOfResidenceImporter(ApiClient api) : base(api, Entity)
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await Api.GetAllAsync<AddressOfResidence>(Entity);
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
            StartDate = startDate,
            ExpirationDate = expirationDate,
            Lodging = (type == ResidenceType.Lodging && lodgingId.HasValue) ? new { ID = lodgingId.Value } : null
        };

        try
        {
            var created = await Api.CreateAsync<AddressOfResidence>(Entity, payload);
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
                    StartDate = record.StartDate,
                    ExpirationDate = record.ExpirationDate,
                    Lodging = record.Lodging != null ? new { ID = record.Lodging.Id } : null
                };

                await Api.CreateAsync<AddressOfResidence>(Entity, payload);
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
        await Api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted AddressOfResidence {id}\n");
    }
}