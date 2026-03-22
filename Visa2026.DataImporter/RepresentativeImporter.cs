using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class RepresentativeImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Representative";

    public RepresentativeImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<Representative>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var companyName = item.Company?.Name ?? "Unknown Company";
            Console.WriteLine($"  [{item.Id}] {item.FullName} for {companyName} (Active: {item.IsActive})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Representative?> CreateOneAsync(
        Guid companyId,
        bool isLocalEmployee,
        Guid? employeeId = null,
        Guid? localEmployeeId = null)
    {
        Console.WriteLine($"=== POST {Entity} ===");

        var payload = new
        {
            Company = new { ID = companyId },
            IsLocalEmployee = isLocalEmployee,
            Employee = (!isLocalEmployee && employeeId.HasValue) ? new { ID = employeeId.Value } : null,
            LocalEmployee = (isLocalEmployee && localEmployeeId.HasValue) ? new { ID = localEmployeeId.Value } : null,
            IsActive = true
        };

        try
        {
            var created = await _api.CreateAsync<Representative>(Entity, payload);
            Console.WriteLine($"  Created Representative ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating Representative: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Representative> records, Guid defaultCompanyId)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    // Use the record's company if set, otherwise fall back to default
                    Company = record.Company != null ? new { ID = record.Company.Id } : new { ID = defaultCompanyId },
                    IsLocalEmployee = record.IsLocalEmployee,
                    Employee = record.Employee != null ? new { ID = record.Employee.Id } : null,
                    LocalEmployee = record.LocalEmployee != null ? new { ID = record.LocalEmployee.Id } : null,
                    IsActive = record.IsActive
                };

                await _api.CreateAsync<Representative>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Representative: {record.FullName}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import for '{record.FullName}': {ex.Message}");
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
        Console.WriteLine($"  Deleted Representative {id}\n");
    }
}