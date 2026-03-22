using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class InvitationItemImporter
{
    private readonly ApiClient _api;
    private const string Entity = "InvitationItem";

    public InvitationItemImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<InvitationItem>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var invNum = item.Invitation?.InvitationNumber ?? "No Inv";
            var personName = item.Person?.FullName ?? "Unknown Person";
            Console.WriteLine($"  [{item.Id}] Inv: {invNum} - {personName} (Passport: {item.Passport?.PassportNumber})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<InvitationItem?> CreateOneAsync(
        Guid invitationId,
        Guid personId,
        Guid passportId,
        bool isUsed = false)
    {
        Console.WriteLine($"=== POST {Entity} ===");

        var payload = new
        {
            Invitation = new { ID = invitationId },
            Person = new { ID = personId },
            Passport = new { ID = passportId },
            IsUsed = isUsed,
            IsCancelled = false,
            IsChanged = false,
            IsActive = true
        };

        try
        {
            var created = await _api.CreateAsync<InvitationItem>(Entity, payload);
            Console.WriteLine($"  Created InvitationItem ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating InvitationItem: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<InvitationItem> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Invitation = record.Invitation != null ? new { ID = record.Invitation.Id } : null,
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    Passport = record.Passport != null ? new { ID = record.Passport.Id } : null,
                    IsUsed = record.IsUsed,
                    IsCancelled = record.IsCancelled,
                    IsChanged = record.IsChanged,
                    IsActive = record.IsActive
                };

                await _api.CreateAsync<InvitationItem>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Item for: {record.Person?.FullName ?? "Unknown"}");
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
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted InvitationItem {id}\n");
    }
}