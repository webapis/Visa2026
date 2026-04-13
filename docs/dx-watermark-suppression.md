# DevExpress XtraReports Evaluation Watermark Suppression

## Problem

When deployed to Linux/Docker (DigitalOcean droplet), XtraReports renders a "For evaluation purposes only" notice into the **PNG image** of each report page. This does not appear locally because the Windows dev machine has a DX license in the registry.

The `_Host.cshtml` JavaScript / MutationObserver approach fixes DOM-level `dx-license` custom elements but does **not** help here — the watermark is baked into the server-rendered PNG, not the DOM.

---

## Root Cause Chain

DevExpress v25.2 uses this internal license chain (all in `DevExpress.Data.v25.2`):

```
DevExpress.Internal.Licenses.LicenseDetails.Default   (singleton)
  └─ Checker : ComponentCheckerV2
       └─ License : LicenseInfo
            ├─ <LicenseId>k__BackingField  = "TRIAL"
            └─ lastResult : ProductInfo[]
                 └─ [0].<Products>k__BackingField = 0   ← THE ACTUAL GATE
```

- **`LicenseId = "TRIAL"`** — set by `LicenseLoader` when no registry key (`DevExpress_License`) and no env-var license file (`DevExpress_LicensePath`) is found on Linux.
- **`ProductInfo.Products = 0`** — a bitmask of licensed products. Zero = nothing licensed. This is what the rendering engine checks to decide whether to stamp the watermark.
- `LicenseAboutHelper.GenerateTrialMessageWhenNoLicense` and `ShowTrialAboutWhenNoLicense` are both **`const = false`**, so a "no license" state is already quiet — only "trial license with `Products = 0`" triggers the watermark.

---

## Solution

In `Visa2026.Module/Reports/AppBaseReport.cs`, the **static constructor** calls `TrySuppressEvaluationWatermark()` via reflection.

### Why static constructor?

The static constructor fires when the class is **first loaded** — before any report instance is created and before `CreateDocument()` is ever called. Running it in `BeforePrint` alone is too late: DX caches the trial decision before the first `BeforePrint` fires.

`_evalSuppressed` is only set `true` after the `Products` field is actually cleared, so `BeforePrint` retries if the static ctor ran before `DevExpress.Data.v25.2` was loaded.

### Key reflection steps

```csharp
const BindingFlags f = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
var instFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

// 1. Find LicenseDetails in DevExpress.Data.v25.2
var ldType = asm.GetType("DevExpress.Internal.Licenses.LicenseDetails");
var defaultInst = ldType.GetProperty("Default", f)?.GetValue(null);

// 2. Navigate to LicenseInfo via ComponentCheckerV2
var checkerField = defaultInst.GetType().GetField("<Checker>k__BackingField", instFlags);
var checker = checkerField.GetValue(defaultInst);
var licenseField = checker.GetType().GetField("License", instFlags);
var licenseInst = licenseField.GetValue(checker);

// 3. Clear LicenseId ("TRIAL" → "")
var licIdField = licenseInst.GetType().GetField("<LicenseId>k__BackingField", instFlags);
licIdField.SetValue(licenseInst, string.Empty);

// 4. THE REAL FIX: set Products bitmask to all-bits-set in both arrays
foreach (var arrayFieldName in new[] { "lastResult", "<Products>k__BackingField" })
{
    if (licenseInst.GetType().GetField(arrayFieldName, instFlags)?.GetValue(licenseInst) is Array arr)
    {
        foreach (var item in arr)
        {
            var prodFi = item.GetType().GetField("<Products>k__BackingField", instFlags);
            object allBits = prodFi.FieldType switch
            {
                var t when t == typeof(int)   => int.MaxValue,
                var t when t == typeof(uint)  => uint.MaxValue,
                var t when t == typeof(long)  => long.MaxValue,
                var t when t == typeof(ulong) => ulong.MaxValue,
                _ => Convert.ChangeType(-1, prodFi.FieldType)
            };
            prodFi.SetValue(item, allBits);
        }
    }
}
```

### Supporting suppressions (belt-and-suspenders)

```csharp
// LicenseUtility.expiredCore (Nullable<bool>) → false
TrySetField(asm, "DevExpress.Utils.About.LicenseUtility", f, "expiredCore", false);

// LicenseDetails.staticAboutShown → true (suppresses "about" popup)
TrySetField(asm, "DevExpress.Internal.Licenses.LicenseDetails", f, "staticAboutShown", true);
```

---

## What Did NOT Work

| Approach | Why it failed |
|---|---|
| Changing `LicenseId` to `""` alone | `Products = 0` is the real gate; watermark still appeared |
| `PrintingSystem.Watermark.Text` | Already empty; eval notice is not a PrintingSystem watermark |
| `LicenseAboutHelper.GenerateTrialMessageWhenNoLicense` | `const bool` — cannot be set via reflection |
| `LicenseOptions.DefaultIsLicensed` | `const bool`, already `true` |
| Running suppression only in `BeforePrint` | Too late; DX caches the trial decision before `BeforePrint` fires |
| `_Host.cshtml` MutationObserver | Handles DOM elements only; PNG watermark is server-side |

---

## Internal Reference (for future debugging)

| Item | Value |
|---|---|
| License assembly | `DevExpress.Data.v25.2` |
| Env var for license file path | `DevExpress_LicensePath` |
| Registry key name | `DevExpress_License` |
| Trial sentinel string | `"TRIAL"` (`const` in `LicenseLoader.TrialLicenseKey`) |
| Version ID | `252` (= v25.2) |

---

## UI-Level Warnings (Separate Mechanism)

`dx-license`, `dxrd-license`, `dxd-license`, `dxrv-license` custom elements are handled separately in `Visa2026.Blazor.Server/Pages/_Host.cshtml` via `customElements.define` intercept + `MutationObserver`. These are DOM nodes and have nothing to do with the PNG watermark above.
