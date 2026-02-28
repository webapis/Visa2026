# Interface: IExpirationLogic

## 1. Overview
The `IExpirationLogic` interface defines a standard contract for any Business Object that has a limited validity period and requires tracking of its expiration status.

## 2. Purpose & Problem Solved

In a complex system like Visa2026, many different documents (Visas, Passports, Work Permits) expire. However, they often have different internal structures:
*   **Naming Inconsistencies**: Some objects use `ExpirationDate`, while others use `EndDate`.
*   **Scattered Logic**: Without a common interface, code to check for expiration (e.g., highlighting rows in red, sending email notifications) would need to be repeated for every single business object type.

**The `IExpirationLogic` interface solves these problems by:**
1.  **Unifying the Contract**: It forces all implementing objects to expose a standard `ExpirationDate` property, regardless of what their internal database column is named.
2.  **Enabling Generic Behavior**: It allows developers to write a single **View Controller** or **Background Service** that works with `IExpirationLogic` objects. This single piece of code can handle highlighting, filtering, and notifications for *all* document types at once.
3.  **Standardizing State**: It enforces the implementation of `DaysRemaining` and `ExpirationState`, ensuring consistent calculation logic across the entire application.

## 3. Interface Contract

```csharp
public interface IExpirationLogic
{
    bool IsActive { get; set; }
    DateTime? ExpirationDate { get; }
    int DaysRemaining { get; }
    ExpirationState ExpirationState { get; }
}
```

*   **`IsActive`**: Indicates if the record is the currently valid one (priority over date).
*   **`ExpirationDate`**: The unified date property used for calculations. Objects with `EndDate` map that property to this interface member.
*   **`DaysRemaining`**: A calculated integer (`ExpirationDate - Today`).
*   **`ExpirationState`**: An enum (`Active`, `ExpiringSoon`, `Expired`, `Archived`) used for UI conditional appearance (color coding).

## 4. Implementing Business Objects

The following Business Objects currently implement this interface:

| Business Object | Original Date Property | Notes |
| :--- | :--- | :--- |
| **Visa** | `ExpirationDate` | - |
| **WorkPermitItem** | `ExpirationDate` | - |
| **Passport** | `ExpirationDate` | - |
| **Invitation** | `ExpirationDate` | - |
| **AddressOfResidence** | `ExpirationDate` | - |
| **MedicalRecord** | `ExpirationDate` | - |
| **EmployeeContract** | `ExpirationDate` | - |

## 5. Usage Examples

*   **Conditional Appearance**: A View Controller checks `ExpirationState` to highlight rows in List Views (Red for Expired, Yellow for Expiring Soon).
*   **Dashboards**: A single dashboard widget can query all objects implementing `IExpirationLogic` to show upcoming expirations.
*   **Notifications**: A background job iterates through these objects to send email alerts 30 days before expiration.