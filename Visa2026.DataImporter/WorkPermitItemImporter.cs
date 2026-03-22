using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class WorkPermitItemImporter
{
    private readonly ApiClient _api;
    private const string Entity = "WorkPermitItem";

    public WorkPermitItemImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<WorkPermitItem>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            Console.WriteLine($"  [{item.Id}] Person: {item.Person?.FullName}, WP#: {item.WorkPermitNumber}, Exp: {item.ExpirationDate:d}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<WorkPermitItem?> CreateOneAsync(
        Guid workPermitId,
        Guid personId,
        Guid passportId,
        Guid positionHistoryId,
        string wpNumber,
        DateTime startDate,
        DateTime expirationDate)
    {
        Console.WriteLine($"=== POST {Entity}: {wpNumber} ===");

        // We must link all required foreign keys by passing objects with IDs
        var payload = new
        {
            WorkPermit = new { ID = workPermitId },
            Person = new { ID = personId },
            Passport = new { ID = passportId },
            CurrentPositionHistory = new { ID = positionHistoryId },
            WorkPermitNumber = wpNumber,
            StartDate = startDate,
            ExpirationDate = expirationDate,
            // Optional fields can be added here
            IsCancelled = false
        };

        try
        {
            var created = await _api.CreateAsync<WorkPermitItem>(Entity, payload);
            Console.WriteLine($"  Created: {created?.Id}");
            Console.WriteLine();
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating item: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<WorkPermitItem> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                // Construct payload ensuring relationships are mapped via ID if objects exist
                var payload = new
                {
                    WorkPermitNumber = record.WorkPermitNumber,
                    ASNumber = record.ASNumber,
                    StartDate = record.StartDate,
                    ExpirationDate = record.ExpirationDate,
                    IsCancelled = record.IsCancelled,

                    // Relationships: OData requires { "ID": "guid" } for existing lookups
                    WorkPermit = record.WorkPermit != null ? new { ID = record.WorkPermit.Id } : null,
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    Passport = record.Passport != null ? new { ID = record.Passport.Id } : null,
                    CurrentPositionHistory = record.CurrentPositionHistory != null ? new { ID = record.CurrentPositionHistory.Id } : null
                };

                await _api.CreateAsync<WorkPermitItem>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Item for: {record.WorkPermitNumber}");
                success++;
            }
            catch (Exception ex)
            {
                // Common error: Missing required fields (Person, Passport, Position)
                Console.WriteLine($"  ✗ Failed Item '{record.WorkPermitNumber}': {ex.Message}");
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