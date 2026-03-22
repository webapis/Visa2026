using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class EmployeePositionHistoryImporter
{
    private readonly ApiClient _api;
    private const string Entity = "EmployeePositionHistory";

    public EmployeePositionHistoryImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<EmployeePositionHistory>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var posName = item.Position?.Name ?? "No Position";
            var deptName = item.Department?.Name ?? "No Dept";
            Console.WriteLine($"  [{item.Id}] {item.Person?.FullName}: {posName} ({deptName}) since {item.StartDate:d}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<EmployeePositionHistory?> CreateOneAsync(
        Guid personId,
        Guid positionId,
        Guid departmentId,
        DateTime startDate,
        DateTime? endDate = null)
    {
        Console.WriteLine($"=== POST {Entity} ===");

        // Construct the payload with nested objects for required relationships
        var payload = new
        {
            Person = new { ID = personId },
            Position = new { ID = positionId },
            Department = new { ID = departmentId },
            StartDate = startDate,
            EndDate = endDate
        };

        try
        {
            var created = await _api.CreateAsync<EmployeePositionHistory>(Entity, payload);
            Console.WriteLine($"  Created History ID: {created?.Id}");
            Console.WriteLine();
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating history: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<EmployeePositionHistory> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    StartDate = record.StartDate,
                    EndDate = record.EndDate,
                    // Ensure relationships are mapped via ID
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    Position = record.Position != null ? new { ID = record.Position.Id } : null,
                    Department = record.Department != null ? new { ID = record.Department.Id } : null
                };

                await _api.CreateAsync<EmployeePositionHistory>(Entity, payload);
                Console.WriteLine($"  ✓ Imported history for: {record.Person?.FullName ?? "Unknown Person"}");
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

    public async Task DeleteAsync(Guid id)
    {
        Console.WriteLine($"=== DELETE {Entity} {id} ===");
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted.\n");
    }
}