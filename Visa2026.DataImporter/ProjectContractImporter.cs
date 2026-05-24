using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class ProjectContractImporter
{
    private readonly ApiClient _api;
    private const string Entity = "ProjectContract";

    public ProjectContractImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<ProjectContract>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            Console.WriteLine($"  [{item.Id}] {item.Name} ({item.Code})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<ProjectContract?> CreateOneAsync(
        string name,
        string code,
        string description,
        bool isDefault = false)
    {
        Console.WriteLine($"=== POST {Entity}: {name} ===");

        var payload = new
        {
            Name = name,
            Code = code,
            Description = description,
            IsDefault = isDefault
        };

        try
        {
            var created = await _api.CreateAsync<ProjectContract>(Entity, payload);
            Console.WriteLine($"  Created ProjectContract ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating ProjectContract: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<ProjectContract> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Name = record.Name,
                    Code = record.Code,
                    Description = record.Description,
                    IsDefault = record.IsDefault
                };

                await _api.CreateAsync<ProjectContract>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Contract: {record.Name}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import for '{record.Name}': {ex.Message}");
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
        Console.WriteLine($"  Deleted ProjectContract {id}\n");
    }
}