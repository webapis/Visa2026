using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public abstract class BaseImporter<T> where T : class
{
    protected readonly ApiClient Api;
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

    protected async Task BulkImportBatchAsync(IEnumerable<T> records, Func<T, object> payloadBuilder)
    {
        Console.WriteLine($"=== Bulk import {EntityName}s (Batch) ===");

        var operations = new List<BatchOperation>();
        foreach (var record in records)
        {
            var payload = payloadBuilder(record);
            operations.Add(new BatchOperation(HttpMethod.Post, $"api/odata/{EntityName}", payload));
        }

        try
        {
            var response = await Api.ExecuteBatchAsync(operations);
            if (response.HasErrors)
            {
                foreach (var item in response.Responses)
                {
                    if (item.StatusCode >= 400)
                    {
                        Console.WriteLine($"  ✗ Batch item failed (Status: {item.StatusCode}): {item.Body}");
                    }
                }
            }
            Console.WriteLine($"  ✓ Batch processed. Total: {operations.Count}, Errors: {response.Responses.Count(r => r.StatusCode >= 400)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Batch request failed: {ex.Message}");
        }
        Console.WriteLine($"  Done.\n");
    }

    /// <summary>
    /// Helper method to perform a lookup by Code or Name, with optional caching.
    /// </summary>
    protected async Task<TLookup?> LookupAsync<TLookup>(string entityName, string lookupValue, bool byCode = true) where TLookup : class
    {
        if (string.IsNullOrWhiteSpace(lookupValue)) return null;

        string filter = byCode ? $"Code eq '{lookupValue}'" : $"Name eq '{lookupValue}'";
        try
        {
            var items = await Api.QueryAsync<TLookup>(entityName, $"$filter={filter}");
            return items.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Error during lookup for {entityName} with {filter}: {ex.Message}");
            return null;
        }
    }


}