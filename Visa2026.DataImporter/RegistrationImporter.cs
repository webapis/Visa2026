using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class RegistrationImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Registration";

    public RegistrationImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<Registration>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var personName = item.Person?.FullName ?? "Unknown Person";
            Console.WriteLine($"  [{item.Id}] {personName} - App: {item.Application?.FullApplicationNumber}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Registration?> CreateOneAsync(
        Guid personId,
        Guid applicationId)
    {
        Console.WriteLine($"=== POST {Entity} for Person ID: {personId} ===");

        var payload = new
        {
            Person = new { ID = personId },
            Application = new { ID = applicationId }
        };

        try
        {
            var created = await _api.CreateAsync<Registration>(Entity, payload);
            Console.WriteLine($"  Created Registration ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating Registration: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Registration> records)
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
                    Application = record.Application != null ? new { ID = record.Application.Id } : null
                };

                await _api.CreateAsync<Registration>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Registration for: {record.Person?.FullName ?? "Unknown"}");
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
        Console.WriteLine($"  Deleted Registration {id}\n");
    }
}