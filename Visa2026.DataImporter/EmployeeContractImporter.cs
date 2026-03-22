using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class EmployeeContractImporter
{
    private readonly ApiClient _api;
    private const string Entity = "EmployeeContract";

    public EmployeeContractImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<EmployeeContract>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var personName = item.Person?.FullName ?? "Unknown";
            var posTitle = item.PositionHistory?.Position?.Name ?? "Unknown Position";
            Console.WriteLine($"  [{item.Id}] {personName} - {posTitle} (Start: {item.ContractStartDate:d}, Salary: {item.Salary:C})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<EmployeeContract?> CreateOneAsync(
        Guid personId,
        Guid positionHistoryId,
        Guid validityDurationId,
        DateTime startDate,
        decimal salary)
    {
        Console.WriteLine($"=== POST {Entity} ===");

        // Construct the payload mapping relationships via ID
        var payload = new
        {
            Person = new { ID = personId },
            PositionHistory = new { ID = positionHistoryId },
            ValidityDuration = new { ID = validityDurationId },
            ContractStartDate = startDate,
            Salary = salary
        };

        try
        {
            var created = await _api.CreateAsync<EmployeeContract>(Entity, payload);
            Console.WriteLine($"  Created Contract ID: {created?.Id}");
            Console.WriteLine();
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating contract: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<EmployeeContract> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    ContractStartDate = record.ContractStartDate,
                    Salary = record.Salary,
                    // Map relationships by ID if the source object is present
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    PositionHistory = record.PositionHistory != null ? new { ID = record.PositionHistory.Id } : null,
                    ValidityDuration = record.ValidityDuration != null ? new { ID = record.ValidityDuration.Id } : null
                };

                await _api.CreateAsync<EmployeeContract>(Entity, payload);
                Console.WriteLine($"  ✓ Imported contract for: {record.Person?.FullName ?? "Unknown Person"}");
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