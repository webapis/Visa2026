# EF Core Change Tracking & INotifyCollectionChanged

## 1. The Error

When working with Entity Framework Core (EF Core) in XAF, you may encounter the following error:

> **ActionExecutionException:** The collection type `List<T>` being used for navigation `Person.PositionHistory` does not implement `INotifyCollectionChanged`. Any entity type configured to use the `ChangingAndChangedNotificationsWithOriginalValues` change tracking strategy must use collections that implement `INotifyCollectionChanged`.

This document explains why this happens and how to fix it properly.

## 2. Root Cause: Change Tracking Strategies

EF Core needs to know when your objects change so it can generate the correct SQL `UPDATE`, `INSERT`, or `DELETE` statements. It has two main ways of doing this:

### A. Snapshot Tracking (Standard EF Core Default)
1.  EF loads an entity and takes a "snapshot" of all its values.
2.  When you call `SaveChanges()`, EF compares the current values against the snapshot.
3.  **Drawback:** For collections (e.g., `myPerson.Passports.Add(...)`), EF doesn't know you added an item unless it scans the entire collection and compares it to the database state. This is inefficient for complex object graphs.

### B. Notification Tracking (XAF Default)
XAF configures EF Core to use the **`ChangingAndChangedNotificationsWithOriginalValues`** strategy.
1.  Instead of EF scanning for changes, the **entity itself tells EF** when something changes.
2.  For simple properties (`string`, `int`), this is handled by the proxy classes XAF generates (implementing `INotifyPropertyChanged`).
3.  For collections, EF relies on the `INotifyCollectionChanged` interface.

## 3. Why List<T> Fails

When you define a property like this:

```csharp
public virtual IList<Passport> Passports { get; set; } = new List<Passport>();
```

Or if you rely on EF Core to materialize the list (which defaults to `List<T>`), the following happens:

1.  You add an item: `myPerson.Passports.Add(newPassport)`.
2.  `List<T>` adds the item internally.
3.  **Crucially:** `List<T>` does **not** fire any event to say "I changed!".
4.  EF Core's change tracker is listening for a notification that never comes.
5.  EF Core throws the `InvalidOperationException` because it cannot guarantee data integrity without those notifications.

## 4. The Solution: ObservableCollection

To fix this, we must use `ObservableCollection<T>`. This class implements `INotifyCollectionChanged` and fires an event whenever an item is added, removed, or the list is cleared.

### The Pattern

**Do not** initialize collections inline (e.g., `public virtual IList<T> Items { get; set; } = new ...`). EF Core might overwrite inline initializers with a standard `List<T>` during materialization.

**Do** initialize all collections in the **constructor**.

### Correct Implementation Example

```csharp
using System.Collections.ObjectModel; // Required namespace

public class Person : BaseObject
{
    // 1. Initialize in the constructor
    public Person()
    {
        Passports = new ObservableCollection<Passport>();
        Educations = new ObservableCollection<Education>();
    }

    // 2. Define as virtual IList<T>
    [InverseProperty(nameof(Passport.Person))]
    [Aggregated]
    public virtual IList<Passport> Passports { get; set; }

    [InverseProperty(nameof(Education.Person))]
    [Aggregated]
    public virtual IList<Education> Educations { get; set; }
}
```

## 5. Summary

1.  XAF uses **Notification Tracking** for performance.
2.  Notification Tracking requires collections to shout "I Changed!"
3.  `List<T>` is silent; `ObservableCollection<T>` shouts.
4.  Always initialize collections as `ObservableCollection<T>` in the **Constructor**.