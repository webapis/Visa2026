using System;

namespace Visa2026.DataImporter;

public static class ApplicationImporterTests
{
    public static void Run()
    {
        Console.WriteLine("=== Running ApplicationImporter Tests ===");

        Guid defaultId = Guid.NewGuid();
        Guid explicitId = Guid.NewGuid();
        Guid inferredId = Guid.NewGuid();

        // Test 1: Fallback to default
        var app1 = new Application(); // No filter, no type
        var result1 = ApplicationImporter.ResolveFilterId(app1, defaultId);
        Assert(result1 == defaultId, "Test 1 Failed: Should return default ID");

        // Test 2: Inferred from ApplicationType
        var app2 = new Application
        {
            ApplicationType = new ApplicationType
            {
                ApplicationTypeFilter = new ApplicationTypeFilter { Id = inferredId }
            }
        };
        var result2 = ApplicationImporter.ResolveFilterId(app2, defaultId);
        Assert(result2 == inferredId, "Test 2 Failed: Should return inferred ID from ApplicationType");

        // Test 3: Explicit overrides Inferred
        var app3 = new Application
        {
            ApplicationTypeFilter = new ApplicationTypeFilter { Id = explicitId },
            ApplicationType = new ApplicationType
            {
                ApplicationTypeFilter = new ApplicationTypeFilter { Id = inferredId }
            }
        };
        var result3 = ApplicationImporter.ResolveFilterId(app3, defaultId);
        Assert(result3 == explicitId, "Test 3 Failed: Explicit ID should override inferred ID");

        Console.WriteLine("=== All Tests Passed ===\n");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {message}");
            Console.ResetColor();
        }
    }
}