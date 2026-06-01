# Interface: IExpirationLogic

## 1. Overview

The `IExpirationLogic` interface defines a standard contract for business objects whose **validity states** use the **`DaysRemaining`** temporal type — a countdown to `ExpirationDate`.

> **Temporal types:** See **[`docs/BO_STATE_TEMPORAL_TYPES.md`](../../docs/BO_STATE_TEMPORAL_TYPES.md)** for the full registry (`DaysRemaining` vs `DaysElapsed`). Process/workflow BOs such as **`ApplicationProgress`** use **`DaysElapsed`** instead and do **not** implement this interface.

## 2. Purpose & Problem Solved

Many documents (visas, passports, work permits, medical records) share the same countdown pattern. The interface:

1. **Unifies the contract** — standard `ExpirationDate` and `DaysRemaining`.
2. **Enables generic behavior** — one evaluator/notification path for all validity BOs.
3. **Links to officer config** — thresholds via [`ExpirationAlertRule`](ExpirationAlertRule.cs) (per-BO `ExpiringSoonDays`, optional `ExtensionApplicationRequiredDays`).

## 3. Interface Contract

```csharp
public interface IExpirationLogic
{
    DateTime? ExpirationDate { get; }
    int DaysRemaining { get; }  // (ExpirationDate.Date - Today).Days
}
```

Optional on implementing classes (not on the interface):

- **`ExpirationState`** — `Active` / `ExpiringSoon` / `Expired` via [`ExpirationLogicHelper`](IExpirationLogic.cs)
- **Evaluators** — richer codes (`ExtensionApplicationRequired`, `Archived`, flags) under `Services/StateEvaluation/Evaluators/`

## 4. Shared logic

[`ExpirationLogicHelper.CalculateExpirationState`](IExpirationLogic.cs) uses per-BO rules from [`ExpirationAlertRule`](ExpirationAlertRule.cs) (days-only window).

## 5. Implementing business objects (`DaysRemaining`)

| Business object | Anchor | Alert rule key |
|-----------------|--------|----------------|
| **Visa** | `ExpirationDate` | `Visa` |
| **Passport** | `ExpirationDate` | `Passport` |
| **WorkPermitItem** | `ExpirationDate` | `WorkPermitItem` |
| **EmployeeContract** | `ExpirationDate` | `EmployeeContract` |
| **MedicalRecord** | `ExpirationDate` | `MedicalRecord` |
| **AddressOfResidence** | `ExpirationDate` | `AddressOfResidence` |
| **Invitation** | `ExpirationDate` | `Invitation` |
| **BorderZone** | `ExpirationDate` | `BorderZone` |

**Not** implementing this interface: **`Application`**, **`ApplicationProgress`** (workflow — **`DaysElapsed`** on `ApplicationProgress.Date`).

## 6. Usage examples

- **ListView row severity** — `Visa.StateSeverityLevel`, evaluators + `ExpirationAlertRule`
- **State notifications** — `ValidityState` category in the inbox plan
- **Officer configuration** — System → Expiration alert rules
