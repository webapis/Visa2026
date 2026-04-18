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
            Console.WriteLine($"  [{item.Id}]");
        }
        Console.WriteLine();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted BusinessTrip {id}\n");
    }
}
