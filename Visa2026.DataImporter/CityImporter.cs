using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class CityImporter : BaseImporter<City>
{
    public CityImporter(ApiClient api) : base(api, "City")
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public Task ListAllAsync()
    {
        return base.ListAllAsync(item => {
            var regionName = item.Region?.Name ?? "No Region";
            Console.WriteLine($"  [{item.Id}] {item.Name} ({regionName}) - Code: {item.Code}");
        });
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
        Console.WriteLine($"=== POST {EntityName}: {name} ===");

        var payload = new
        {
            Name = name,
            NameTm = nameTm,
            Code = code,
            Region = new { ID = regionId },
            IsDefault = isDefault,
            PdfForm_Code = pdfFormCode
        };

        try
        {
            var created = await Api.CreateAsync<City>(EntityName, payload);
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
        await BulkImportLoopAsync(
            records, 
            payloadBuilder: record => new
            {
                Name = record.Name,
                NameTm = record.NameTm,
                Code = record.Code,
                IsDefault = record.IsDefault,
                PdfForm_Code = record.PdfFormCode,
                Region = record.Region != null ? new { ID = record.Region.Id } : null
            },
            nameSelector: record => record.Name);
    }
}