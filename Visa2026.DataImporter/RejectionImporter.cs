using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class RejectionImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Rejection";

    public RejectionImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<Rejection>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var appNum = item.Application?.ApplicationNumber ?? "No App";
            Console.WriteLine($"  [{item.Id}] Doc#: {item.RejectedDocNumber} (App: {appNum}) on {item.Date:d}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Rejection?> CreateOneAsync(
        Guid applicationId,
        string rejectedDocNumber,
        string reason,
        DateTime date)
    {
        Console.WriteLine($"=== POST {Entity}: {rejectedDocNumber} ===");

        var payload = new
        {
            Application = new { ID = applicationId },
            RejectedDocNumber = rejectedDocNumber,
            Reason = reason,
            Date = date
        };

        try
        {
            var created = await _api.CreateAsync<Rejection>(Entity, payload);
            Console.WriteLine($"  Created Rejection ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating Rejection: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Rejection> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Application = record.Application != null ? new { ID = record.Application.Id } : null,
                    RejectedDocNumber = record.RejectedDocNumber,
                    Reason = record.Reason,
                    Date = record.Date
                };

                await _api.CreateAsync<Rejection>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Rejection: {record.RejectedDocNumber}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed '{record.RejectedDocNumber}': {ex.Message}");
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
        Console.WriteLine($"  Deleted Rejection {id}\n");
    }
}