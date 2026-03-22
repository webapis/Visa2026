using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class MedicalRecordImporter
{
    private readonly ApiClient _api;
    private const string Entity = "MedicalRecord";

    public MedicalRecordImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<MedicalRecord>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var personName = item.Person?.FullName ?? "Unknown Person";
            Console.WriteLine($"  [{item.Id}] {personName} - Doc#: {item.DocumentNumber} (Issued: {item.IssueDate:d})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<MedicalRecord?> CreateOneAsync(
        Guid personId,
        string documentNumber,
        DateTime issueDate,
        Guid validityDurationId)
    {
        Console.WriteLine($"=== POST {Entity} for Person ID: {personId} ===");

        var payload = new
        {
            Person = new { ID = personId },
            DocumentNumber = documentNumber,
            IssueDate = issueDate,
            ValidityDuration = new { ID = validityDurationId },
            IsActive = true
        };

        try
        {
            var created = await _api.CreateAsync<MedicalRecord>(Entity, payload);
            Console.WriteLine($"  Created MedicalRecord ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating MedicalRecord: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<MedicalRecord> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    DocumentNumber = record.DocumentNumber,
                    IssueDate = record.IssueDate,
                    ValidityDuration = record.ValidityDuration != null ? new { ID = record.ValidityDuration.Id } : null,
                    IsActive = record.IsActive
                };

                await _api.CreateAsync<MedicalRecord>(Entity, payload);
                Console.WriteLine($"  ✓ Imported MedicalRecord: {record.DocumentNumber}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import for '{record.DocumentNumber}': {ex.Message}");
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
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted MedicalRecord {id}\n");
    }
}