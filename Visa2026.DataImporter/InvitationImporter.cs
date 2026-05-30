using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class InvitationImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Invitation";

    public InvitationImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<Invitation>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var appNum = item.Application?.ApplicationNumber ?? "No App";
            Console.WriteLine($"  [{item.Id}] Inv#: {item.InvitationNumber} (App: {appNum}) - Starts: {item.StartDate:d}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Invitation?> CreateOneAsync(
        string invitationNumber,
        DateTime startDate,
        Guid applicationId,
        Guid validityDurationId)
    {
        Console.WriteLine($"=== POST {Entity}: {invitationNumber} ===");

        var payload = new
        {
            InvitationNumber = invitationNumber,
            StartDate = startDate,
            
            // Required Relationships
            Application = new { ID = applicationId },
            ValidityDuration = new { ID = validityDurationId },

            // Defaults
            IsCancelled = false
        };

        try
        {
            var created = await _api.CreateAsync<Invitation>(Entity, payload);
            Console.WriteLine($"  Created Invitation ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating Invitation: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Invitation> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    InvitationNumber = record.InvitationNumber,
                    StartDate = record.StartDate,
                    IsCancelled = record.IsCancelled,
                    IsChanged = record.IsChanged,

                    Application = record.Application != null ? new { ID = record.Application.Id } : null,
                    ValidityDuration = record.ValidityDuration != null ? new { ID = record.ValidityDuration.Id } : null
                };

                await _api.CreateAsync<Invitation>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Invitation: {record.InvitationNumber}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed '{record.InvitationNumber}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    public async Task DeleteAsync(Guid id)
    {
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted Invitation {id}\n");
    }
}