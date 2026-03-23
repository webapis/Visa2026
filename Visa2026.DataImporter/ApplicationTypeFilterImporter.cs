using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class ApplicationTypeFilterImporter : BaseImporter<ApplicationTypeFilter>
{
    public ApplicationTypeFilterImporter(ApiClient api) : base(api, "ApplicationTypeFilter")
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public Task ListAllAsync()
    {
        return base.ListAllAsync(item => 
            Console.WriteLine($"  [{item.Id}] {item.Name} (Category: {item.Category}) - Code: {item.Code}"));
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<ApplicationTypeFilter?> CreateOneAsync(
        string name,
        string nameTm,
        string code,
        ApplicationTypeCategory category,
        bool isDefault = false)
    {
        Console.WriteLine($"=== POST {EntityName}: {name} ===");

        var payload = new
        {
            Name = name,
            NameTm = nameTm,
            Code = code,
            Category = category,
            IsDefault = isDefault
        };

        try
        {
            var created = await Api.CreateAsync<ApplicationTypeFilter>(EntityName, payload);
            Console.WriteLine($"  Created {EntityName} ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating {EntityName}: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<ApplicationTypeFilter> records)
    {
        await BulkImportLoopAsync(records, record => new
        {
            Name = record.Name,
            NameTm = record.NameTm,
            Code = record.Code,
            Category = record.Category,
            IsDefault = record.IsDefault
        });
    }
}