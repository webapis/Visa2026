# Integration Guide: MSSQL Views as Business Objects (Visa2026)

This guide explains how to map an SQL Server View to an XAF Business Object in this project.

## 1. Create/Update the SQL View (SqlViewsUpdater)

In this project, we manage SQL Views using a dedicated updater class: `Visa2026.Module.DatabaseUpdate.SqlViewsUpdater.cs`.
This allows us to use `CREATE OR ALTER VIEW` syntax, making it easier to modify views during development without creating a new EF Core migration for every change.

1.  Open `Visa2026.Module\DatabaseUpdate\SqlViewsUpdater.cs`.
2.  Add a new method `CreateView_YourViewName()`.
3.  Call `ExecuteNonQueryCommand` with the SQL definition.

*Example:*
```csharp
private void CreateViewVisaExtensionTracking()
{
    ExecuteNonQueryCommand(@"
        CREATE OR ALTER VIEW [dbo].[View_VisaExtensionTracking] AS
        SELECT ...
    ", true);
}
```

## 2. Define the Business Object

Create a class in `Visa2026.Module\BusinessObjects`. It does **not** need to inherit from `BaseObject` if the View provides its own ID, but inheriting from `BaseObject` is fine if the View's ID column maps to `ID`.

**Important**: The class must have a `[Key]` property. XAF uses this key to generate the "Open Record" link.

```csharp
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.DC;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Visa2026.Module.BusinessObjects
{
    // NavigationItem adds it to the UI menu
    [NavigationItem("Reports")] 
    [DomainComponent] // Optional: Helps XAF treat it as a non-creatable entity in some contexts
    public class EmployeeSummary
    {
        [Key]
        // If the view column is 'ID' and is a Guid
        public Guid ID { get; set; } 

        public string FullName { get; set; }
        public string CompanyName { get; set; }
        
        [DataType(DataType.Currency)]
        public decimal? Salary { get; set; }
    }
}
```

## 3. Configure the DbContext

You must tell EF Core that this entity maps to a View, not a Table. This prevents EF from trying to create a table named `EmployeeSummary`.

Open `Visa2026.Module\BusinessObjects\Visa2026EFCoreDbContext.cs` (or your specific DbContext file) and update `OnModelCreating`:

```csharp
public DbSet<EmployeeSummary> EmployeeSummaries { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Map the entity to the SQL View Name
    modelBuilder.Entity<EmployeeSummary>()
        .ToView("View_EmployeeSummary")
        .HasKey(t => t.ID); // Explicitly define the key
}
```

## 4. XAF UI Configuration (Read-Only)

Since SQL Views are typically read-only (unless using specific triggers), you should configure the XAF View to prevent editing.

### Option A: Attributes (Code)
Add `[ModelDefault("AllowEdit", "False")]` to the class.

```csharp
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class EmployeeSummary { ... }
```

### Option B: Controller
Create a `ViewController` targeting `EmployeeSummary` and set `AllowEdit` to false.

## 5. Troubleshooting "Open Record Link"

If the ListView displays rows but clicking them does **not** open a DetailView:

1.  **Check the Key**: Ensure the `[Key]` property actually contains unique values. If the SQL View returns duplicate IDs, EF Core might behave unpredictably or XAF won't know which record to load.
2.  **DetailView Existence**: Ensure a DetailView exists in the Model (`Model.xafml`). It is usually generated automatically.
3.  **Composite Keys**: If your view lacks a single unique ID, use a composite key in `OnModelCreating`:
    ```csharp
    modelBuilder.Entity<MyView>()
        .HasKey(v => new { v.ColumnA, v.ColumnB });
    ```
    *Note: XAF supports composite keys, but a single unique GUID or Int is preferred for smoother navigation.*

## 6. Server-Side Calculated Fields (Computed Columns)

You can map a property to a SQL Server Scalar-Valued Function (SVF) to perform server-side calculations (sorting, filtering) without loading all records into memory. This serves as a performant alternative to calculated properties that cannot be translated to SQL by EF Core.

### 1. Create the SQL Function
Add the function creation logic to `SqlViewsUpdater.cs`.

```csharp
private void CreateFunctions()
{
    ExecuteNonQueryCommand(@"
        CREATE OR ALTER FUNCTION [dbo].[fn_CalculateDaysRemaining] (@ExpirationDate DATE)
        RETURNS INT
        AS
        BEGIN
            IF @ExpirationDate IS NULL RETURN 0;
            RETURN DATEDIFF(day, GETDATE(), @ExpirationDate);
        END
    ", true);
}
```

### 2. Add Property to Business Object
Add a read-only property to your business object. Use `[DatabaseGenerated(DatabaseGeneratedOption.Computed)]` to indicate that the database handles the value.

```csharp
[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("Caption", "Days Remaining (Server)")]
public virtual int? DaysRemainingServerSide { get; private set; }
```

### 3. Map in DbContext
Register the computed column SQL in `Visa2026DbContext.cs`.

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ...
    modelBuilder.Entity<Visa>()
        .Property(p => p.DaysRemainingServerSide)
        .HasComputedColumnSql("[dbo].[fn_CalculateDaysRemaining]([ExpirationDate])");
}
```