using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class CityImporter
{
    private readonly ApiClient _api;
    private const string Entity = "City";

    public CityImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<City>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var regionName = item.Region?.Name ?? "No Region";
            Console.WriteLine($"  [{item.Id}] {item.Name} ({regionName}) - Code: {item.Code}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<City?> CreateOneAsync(
        string name,
        string nameTm,
        string code,
        Guid regionId,
        bool isDefault = false,
        string pdfFormCode = "")
    {
        Console.WriteLine($"=== POST {Entity}: {name} ===");

        var payload = new
        {
            Name = name,
            NameTm = nameTm,
            Code = code,
            Region = new { ID = regionId },
            IsDefault = isDefault,
            PdfFormCode = pdfFormCode
        };

        try
        {
            var created = await _api.CreateAsync<City>(Entity, payload);
            Console.WriteLine($"  Created City ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating City: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<City> records)
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
                    NameTm = record.NameTm,
                    Code = record.Code,
                    IsDefault = record.IsDefault,
                    PdfFormCode = record.PdfFormCode,
                    Region = record.Region != null ? new { ID = record.Region.Id } : null
                };

                await _api.CreateAsync<City>(Entity, payload);
                Console.WriteLine($"  ✓ Imported City: {record.Name}");
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

    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted City {id}\n");
    }
}