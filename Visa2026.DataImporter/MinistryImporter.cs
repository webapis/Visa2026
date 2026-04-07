using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class MinistryImporter : BaseImporter<Ministry>
{
    public MinistryImporter(ApiClient api) : base(api, "Ministry")
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public Task ListAllAsync()
    {
        return base.ListAllAsync(item =>
            Console.WriteLine($"  [{item.Id}] {item.Name}"));
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Ministry?> CreateOneAsync(
        string name,
        string? recipientBlock = null,
        string? formOfAddress = null)
    {
        Console.WriteLine($"=== POST {EntityName}: {name} ===");

        var payload = new
        {
            Name = name,
            RecipientBlock = recipientBlock,
            FormOfAddress = formOfAddress
        };

        try
        {
            var created = await Api.CreateAsync<Ministry>(EntityName, payload);
            Console.WriteLine($"  Created Ministry ID: {created?.Id}");
            Console.WriteLine();
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating ministry: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Ministry> records)
    {
        await BulkImportLoopAsync(records, record => new
        {
            Name = record.Name,
            RecipientBlock = record.RecipientBlock,
            FormOfAddress = record.FormOfAddress
        });
    }
}
