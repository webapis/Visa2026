namespace Visa2026.DataImporter;

/// <summary>
/// Example importer that demonstrates all CRUD operations on VisaType.
/// Replace the sample data with your real import source
/// (CSV, Excel, another database, etc.)
/// </summary>
public class VisaTypeImporter
{
    private readonly ApiClient _api;
    private const string Entity = "VisaType";

    public VisaTypeImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine("=== GET all VisaTypes ===");
        var items = await _api.GetAllAsync<VisaType>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
            Console.WriteLine($"  {item}");
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // READ — filtered query
    // ------------------------------------------------------------------
    public async Task ListDefaultsAsync()
    {
        Console.WriteLine("=== GET VisaTypes where IsDefault = true ===");
        var items = await _api.QueryAsync<VisaType>(
            Entity, "$filter=IsDefault eq true&$orderby=Name");
        foreach (var item in items)
            Console.WriteLine($"  {item}");
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<VisaType?> CreateOneAsync(string name, string nameTm,
        string code, bool isDefault = false)
    {
        Console.WriteLine($"=== POST VisaType: {name} ===");
        var created = await _api.CreateAsync<VisaType>(Entity, new
        {
            name,
            nameTm,
            code,
            isDefault,
            pdfForm_Code = 0
        });
        Console.WriteLine($"  Created: {created}");
        Console.WriteLine();
        return created;
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<VisaType> records)
    {
        Console.WriteLine("=== Bulk import VisaTypes ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                await _api.CreateAsync<VisaType>(Entity, new
                {
                    name      = record.Name,
                    nameTm    = record.NameTm,
                    code      = record.Code,
                    isDefault = record.IsDefault,
                    pdfForm_Code = record.PdfFormCode
                });
                Console.WriteLine($"  ✓ Imported: {record.Name}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed '{record.Name}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    // ------------------------------------------------------------------
    // UPDATE — patch an existing record
    // ------------------------------------------------------------------
    public async Task UpdateAsync(Guid id, string newName)
    {
        Console.WriteLine($"=== PATCH VisaType {id} ===");
        await _api.UpdateAsync(Entity, id, new { name = newName });
        Console.WriteLine($"  Updated Name to: {newName}\n");
    }

    // ------------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------------
    public async Task DeleteAsync(Guid id)
    {
        Console.WriteLine($"=== DELETE VisaType {id} ===");
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted.\n");
    }
}
