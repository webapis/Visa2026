using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class CompanyImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Company";

    public CompanyImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}ies ===");
        var items = await _api.GetAllAsync<Company>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            Console.WriteLine($"  [{item.Id}] {item.Name} (Prefix: {item.AppNumberPrefix})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Company?> CreateOneAsync(
        string name,
        string address,
        string phoneNumber,
        string email,
        string taxInfo,
        string appNumberPrefix,
        int appNumberPadding,
        bool isDefault)
    {
        Console.WriteLine($"=== POST {Entity}: {name} ===");

        var payload = new
        {
            Name = name,
            Address = address,
            PhoneNumber = phoneNumber,
            Email = email,
            TaxInformation = taxInfo,
            AppNumberPrefix = appNumberPrefix,
            ApplicationNumberPadding = appNumberPadding,
            IsDefault = isDefault
        };

        try
        {
            var created = await _api.CreateAsync<Company>(Entity, payload);
            Console.WriteLine($"  Created Company ID: {created?.Id}");
            Console.WriteLine();
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating company: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Company> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}ies ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Name = record.Name,
                    Address = record.Address,
                    PhoneNumber = record.PhoneNumber,
                    Email = record.Email,
                    TaxInformation = record.TaxInformation,
                    AppNumberPrefix = record.AppNumberPrefix,
                    ApplicationNumberPadding = record.ApplicationNumberPadding,
                    IsDefault = record.IsDefault
                };

                await _api.CreateAsync<Company>(Entity, payload);
                Console.WriteLine($"  ✓ Imported: {record.Name}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed '{record.Name}': {ex.Message}");
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