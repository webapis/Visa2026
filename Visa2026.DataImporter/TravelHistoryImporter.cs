using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class TravelHistoryImporter : BaseImporter<TravelHistory>
{
    private const string Entity = "TravelHistory";

    public TravelHistoryImporter(ApiClient api) : base(api, Entity)
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {EntityName}s ===");
        var items = await Api.GetAllAsync<TravelHistory>(EntityName);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var person = item.Person?.FullName ?? "Unknown";
            Console.WriteLine($"  [{item.Id}] {person}: {item.MovementType} ({item.TravelType}) on {item.TravelDate:d}");
       }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<TravelHistory?> CreateOneAsync(
        string typeName,
        Guid personId,
        DateTime travelDate,
        Guid? checkPointId = null,
        Guid? purposeOfTravelId = null,
        string notes = "")
    {
        Console.WriteLine($"=== POST {Entity} ===");

        var payload = new Dictionary<string, object?>
        {
            ["@odata.type"] = $"#Visa2026.Module.BusinessObjects.{typeName}",
            ["Person"] = new { ID = personId },
            ["TravelDate"] = travelDate,
            ["CheckPoint"] = checkPointId.HasValue ? new { ID = checkPointId.Value } : null,
            ["PurposeOfTravel"] = purposeOfTravelId.HasValue ? new { ID = purposeOfTravelId.Value } : null,
            ["Notes"] = notes
        };

        try
        {
           var created = await Api.CreateAsync<TravelHistory>(EntityName, payload);
            Console.WriteLine($"  Created TravelHistory ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating TravelHistory: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<TravelHistory> records)
    {
        Console.WriteLine($"=== Bulk import {EntityName}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                // For bulk import, we expect the record object to have been 
                // prepared with the correct @odata.type via ValueMap in ExcelMappings
                var payload = new
            {
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    TravelDate = record.TravelDate,
                    
                    CheckPoint = record.CheckPoint != null ? new { ID = record.CheckPoint.Id } : null,
                    PurposeOfTravel = record.PurposeOfTravel != null ? new { ID = record.PurposeOfTravel.Id } : null,
                    Notes = record.Notes
                };

                await Api.CreateAsync<TravelHistory>(EntityName, payload);
                Console.WriteLine($"  ✓ Imported TravelHistory for: {record.Person?.FullName ?? "Unknown"}");
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
        await Api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted TravelHistory {id}\n");
    }
}