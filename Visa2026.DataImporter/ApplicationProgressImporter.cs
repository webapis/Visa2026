using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class ApplicationProgressImporter
{
    private readonly ApiClient _api;
    private const string Entity = "ApplicationProgress";

    public ApplicationProgressImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity} records ===");
        var items = await _api.GetAllAsync<ApplicationProgress>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var appNum = item.Application?.ApplicationNumber ?? "No App";
            var state = item.State?.Name ?? "No State";
            Console.WriteLine($"  [{item.Id}] App: {appNum} -> {state} on {item.Date:g}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<ApplicationProgress?> CreateOneAsync(
        Guid applicationId,
        Guid stateId,
        Guid locationId,
        DateTime date,
        string description = "")
    {
        Console.WriteLine($"=== POST {Entity} for App ID: {applicationId} ===");

        var payload = new
        {
            Application = new { ID = applicationId },
            State = new { ID = stateId },
            Location = new { ID = locationId },
            Date = date,
            Description = description
        };

        try
        {
            var created = await _api.CreateAsync<ApplicationProgress>(Entity, payload);
            Console.WriteLine($"  Created ApplicationProgress ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating ApplicationProgress: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<ApplicationProgress> records)
    {
        Console.WriteLine($"=== Bulk import {Entity} records ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Application = record.Application != null ? new { ID = record.Application.Id } : null,
                    State = record.State != null ? new { ID = record.State.Id } : null,
                    Location = record.Location != null ? new { ID = record.Location.Id } : null,
                    Date = record.Date,
                    Description = record.Description
                };

                await _api.CreateAsync<ApplicationProgress>(Entity, payload);
                Console.WriteLine($"  ✓ Imported progress for App: {record.Application?.ApplicationNumber ?? "Unknown"}");
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
        Console.WriteLine($"  Deleted ApplicationProgress {id}\n");
    }
}