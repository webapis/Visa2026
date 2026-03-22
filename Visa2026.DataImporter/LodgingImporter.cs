using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;
public class LodgingImporter : BaseImporter<Lodging>
{
    private const string Entity = "Lodging";

    public LodgingImporter(ApiClient api) : base(api, Entity)
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()

    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await Api.GetAllAsync<Lodging>(EntityName);
        if (items.Count == 0) {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var companyName = item.Company?.Name ?? "Public";
            Console.WriteLine($"  [{item.Id}] {item.Name} ({companyName}) - {item.FullAddress}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Lodging?> CreateOneAsync(
        string name,
    string fullAddress,
        Guid? companyId = null,
        string notes = "")
    {
        Console.WriteLine($"=== POST {Entity}: {name} ===");
        
        var payload = new
        {
       Name = name,
            FullAddress = fullAddress,
           Notes = notes,
            Company = companyId.HasValue ? new { ID = companyId.Value } : null
        };

        try
     {
            var created = await Api.CreateAsync<Lodging>(EntityName, payload);
            Console.WriteLine($"  Created Lodging ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating Lodging: {ex.Message}");
            return null;
        }
    }
    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Lodging> records)
    {
        Console.WriteLine($"=== Bulk import {EntityName}s ===");
        int success = 0, fail = 0;
  
        foreach (var record in records)
        {
            try
           {
                var payload = new
                {
                    Name = record.Name,
                    FullAddress = record.FullAddress,
                    Notes = record.Notes,
                    Company = record.Company != null ? new { ID = record.Company.Id } : null
                };
                   await Api.CreateAsync<Lodging>(EntityName, payload);
                Console.WriteLine($"  ✓ Imported Lodging: {record.Name}");
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
        await Api.DeleteAsync(EntityName, id);
        Console.WriteLine($"  Deleted Lodging {id}\n");
    }
}