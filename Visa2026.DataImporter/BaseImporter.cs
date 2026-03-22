using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public abstract class BaseImporter<T> where T : class
{
    protected readonly ApiClient Api;
    //use ConcurrentDictionary for thread safety
    private static readonly ConcurrentDictionary<string, object?> _lookupCache = new ConcurrentDictionary<string, object?>();

    protected readonly string EntityName;

    protected BaseImporter(ApiClient api, string entityName)
    {
        Api = api;
        EntityName = entityName;
    }

    public virtual async Task ListAllAsync(Action<T>? printItem = null)
    {
        Console.WriteLine($"=== GET all {EntityName}s ===");
        var items = await Api.GetAllAsync<T>(EntityName);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            if (printItem != null)
                printItem(item);
            else
                Console.WriteLine($"  Item: {item}");
        }
        Console.WriteLine();
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        Console.WriteLine($"=== DELETE {EntityName} {id} ===");
        await Api.DeleteAsync(EntityName, id);
        Console.WriteLine("  Deleted.\n");
    }

    protected async Task BulkImportLoopAsync(IEnumerable<T> records, Func<T, object> payloadBuilder, Func<T, string>? nameSelector = null)
    {
        Console.WriteLine($"=== Bulk import {EntityName}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            string displayName = nameSelector != null ? nameSelector(record) : "Record";
            try
            {
                var payload = payloadBuilder(record);
                await Api.CreateAsync<T>(EntityName, payload);
                Console.WriteLine($"  ✓ Imported {EntityName}: {displayName}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import for '{displayName}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    /// <summary>
    /// Helper method to perform a lookup by Code or Name, with optional caching.
    /// </summary>
    protected async Task<TLookup?> LookupAsync<TLookup>(string entityName, string lookupValue, bool byCode = true) where TLookup : class
    {
        if (string.IsNullOrWhiteSpace(lookupValue)) return null;
        string filter = byCode ? $"Code eq '{lookupValue}'" : $"Name eq '{lookupValue}'";
        string cacheKey = $"{entityName}-{filter}";

        if (_lookupCache.TryGetValue(cacheKey, out object? cachedResult))
        {
            return (TLookup?)cachedResult;
        }

        TLookup? result = null;

       try
       {
            var items = await Api.QueryAsync<TLookup>(entityName, $"$filter={filter}"); 
            result = items.FirstOrDefault();
            _lookupCache.TryAdd(cacheKey, result);
        }
       catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error during lookup for {entityName} with {filter}: {ex.Message}");
            return null;
        }
       

        
        return result;
    }

    public static void ClearLookupCache()
    {
        _lookupCache.Clear();
    }


}