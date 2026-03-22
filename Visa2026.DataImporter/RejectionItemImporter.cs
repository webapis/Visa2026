using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class RejectionItemImporter
{
    private readonly ApiClient _api;
    private const string Entity = "RejectionItem";

    public RejectionItemImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<RejectionItem>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var rejectionNum = item.Rejection?.RejectedDocNumber ?? "No Rejection";
            var personName = item.Person?.FullName ?? "Unknown Person";
            Console.WriteLine($"  [{item.Id}] Rej#: {rejectionNum} - {personName}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<RejectionItem?> CreateOneAsync(
        Guid rejectionId,
        Guid personId,
        string reason)
    {
        Console.WriteLine($"=== POST {Entity} for Person ID: {personId} ===");

        var payload = new
        {
            Rejection = new { ID = rejectionId },
            Person = new { ID = personId },
            Reason = reason
        };

        try
        {
            var created = await _api.CreateAsync<RejectionItem>(Entity, payload);
            Console.WriteLine($"  Created RejectionItem ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating RejectionItem: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<RejectionItem> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Rejection = record.Rejection != null ? new { ID = record.Rejection.Id } : null,
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    Reason = record.Reason
                };

                await _api.CreateAsync<RejectionItem>(Entity, payload);
                Console.WriteLine($"  ✓ Imported RejectionItem for: {record.Person?.FullName ?? "Unknown"}");
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

    // ------------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------------
    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted RejectionItem {id}\n");
    }
}