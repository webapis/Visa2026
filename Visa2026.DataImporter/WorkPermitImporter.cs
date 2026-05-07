using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class WorkPermitImporter
{
    private readonly ApiClient _api;
    private const string Entity = "WorkPermit";

    public WorkPermitImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<WorkPermit>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            Console.WriteLine($"  [{item.Id}] No: {item.WorkPermitNumber}, Issued: {item.IssuedDate:d}, Cancelled: {item.IsCancelled}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<WorkPermit?> CreateOneAsync(string number, DateTime startDate, Guid applicationId)
    {
        Console.WriteLine($"=== POST {Entity}: {number} ===");

        // To link an existing Application, we pass the ID in a nested object.
        var payload = new
        {
            WorkPermitNumber = number,
            IssuedDate = startDate,
            Application = new { ID = applicationId }
        };

        var created = await _api.CreateAsync<WorkPermit>(Entity, payload);
        Console.WriteLine($"  Created: {created?.Id}");
        Console.WriteLine();
        return created;
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<WorkPermit> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                // Prepare payload
                // We ensure the Application relationship is passed correctly if it exists in the source record
                object payload;
                if (record.Application != null)
                {
                    payload = new
                    {
                        WorkPermitNumber = record.WorkPermitNumber,
                        IssuedDate = record.IssuedDate,
                        IsCancelled = record.IsCancelled,
                        Application = new { ID = record.Application.Id }
                    };
                }
                else
                {
                    // Fallback payload (note: server validation might fail if Application is required)
                    payload = new
                    {
                        WorkPermitNumber = record.WorkPermitNumber,
                        IssuedDate = record.IssuedDate,
                        IsCancelled = record.IsCancelled
                    };
                }

                await _api.CreateAsync<WorkPermit>(Entity, payload);
                Console.WriteLine($"  ✓ Imported: {record.WorkPermitNumber}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed '{record.WorkPermitNumber}': {ex.Message}");
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
        Console.WriteLine($"=== DELETE {Entity} {id} ===");
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted.\n");
    }
}