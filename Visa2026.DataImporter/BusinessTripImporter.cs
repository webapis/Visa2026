using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class BusinessTripImporter
{
    private readonly ApiClient _api;
    private const string Entity = "BusinessTrip";

    public BusinessTripImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<BusinessTrip>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            // Note: This assumes the 'Person' property will be added to the BusinessTrip model in Models.cs
            // var personName = item.Person?.FullName ?? "Unknown Person";
            Console.WriteLine($"  [{item.Id}] Purpose: {item.Purpose} to {item.DestinationCity}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<BusinessTrip?> CreateOneAsync(
        Guid personId,
        string purpose,
        Guid destinationCountryId,
        DateTime startDate,
        DateTime endDate,
        BusinessTripStatus status = BusinessTripStatus.Planned,
        string destinationCity = "",
        Guid? applicationId = null)
    {
        Console.WriteLine($"=== POST {Entity} for Person ID: {personId} ===");

        var payload = new
        {
            Person = new { ID = personId },
            Purpose = purpose,
            DestinationCountry = new { ID = destinationCountryId },
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            DestinationCity = destinationCity,
            Application = applicationId.HasValue ? new { ID = applicationId.Value } : null
        };

        try
        {
            var created = await _api.CreateAsync<BusinessTrip>(Entity, payload);
            Console.WriteLine($"  Created BusinessTrip ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating BusinessTrip: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<BusinessTrip> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    // Note: This assumes the 'Person' property will be added to the BusinessTrip model in Models.cs
                    // Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    Purpose = record.Purpose,
                    DestinationCountry = record.DestinationCountry != null ? new { ID = record.DestinationCountry.Id } : null,
                    StartDate = record.StartDate,
                    EndDate = record.EndDate,
                    Status = record.Status,
                    DestinationCity = record.DestinationCity,
                    Application = record.Application != null ? new { ID = record.Application.Id } : null,
                    Address = record.Address != null ? new { 
                        FullAddress = record.Address.FullAddress,
                        City = record.Address.City != null ? new { ID = record.Address.City.Id } : null
                    } : null
                };

                await _api.CreateAsync<BusinessTrip>(Entity, payload);
                Console.WriteLine($"  ✓ Imported BusinessTrip: {record.Purpose}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import for '{record.Purpose}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted BusinessTrip {id}\n");
    }
}