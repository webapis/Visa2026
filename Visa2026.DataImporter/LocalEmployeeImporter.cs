using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class LocalEmployeeImporter
{
    private readonly ApiClient _api;
    private const string Entity = "LocalEmployee";

    public LocalEmployeeImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<LocalEmployee>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            Console.WriteLine($"  [{item.Id}] {item.FullName}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<LocalEmployee?> CreateOneAsync(
        string firstName,
        string lastName,
        Guid companyId)
    {
        Console.WriteLine($"=== POST {Entity}: {firstName} {lastName} ===");

        var payload = new
        {
            FirstName = firstName,
            LastName = lastName,
            Company = new { ID = companyId }
        };

        try
        {
            var created = await _api.CreateAsync<LocalEmployee>(Entity, payload);
            Console.WriteLine($"  Created LocalEmployee ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating LocalEmployee: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<LocalEmployee> records, Guid defaultCompanyId)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                // Note: If the LocalEmployee model in Models.cs gets updated to include 'Company',
                // we can use record.Company?.Id here. For now, we use the defaultCompanyId argument.
                var payload = new
                {
                    FirstName = record.FirstName,
                    LastName = record.LastName,
                    Company = new { ID = defaultCompanyId }
                };

                await _api.CreateAsync<LocalEmployee>(Entity, payload);
                Console.WriteLine($"  ✓ Imported: {record.FirstName} {record.LastName}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import for '{record.FirstName} {record.LastName}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted LocalEmployee {id}\n");
    }
}