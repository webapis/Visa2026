# Visa2026 usage license — login page banner

Host-only notice bar shown at the top of **`/LoginPage`**. It communicates **Visa2026 trial / usage licensing** for this installation (customer evaluation period), **not** DevExpress component licensing.

DevExpress trial UI (`dx-license` elements, evaluation watermarks) is handled separately — see [`dx-watermark-suppression.md`](dx-watermark-suppression.md) and `Visa2026.Blazor.Server/Pages/_Host.cshtml` (license DOM suppression).

---

## Behavior

| Aspect | Detail |
|--------|--------|
| **When shown** | `UsageLicense:Enabled` is `true` **and** a trial end date can be resolved (see below). |
| **Where** | Fixed bar at the top of the page; visible only on `/LoginPage` (hidden after login via route tracking in `_Host.cshtml`). |
| **Content** | Single localized line, e.g. `Deneme lisansı — 90 gün kaldı` or `Deneme lisansının süresi doldu` when expired. |
| **Styling** | Compact “enterprise notice” bar: warm off-white background, left accent stripe, small info icon. Expired state uses a muted red accent. CSS: `wwwroot/css/site.css` (`.usage-license-banner`). |

The banner is **informational**. It does not block login or enforce license server-side. Any hard enforcement must be added separately if required.

---

## Configuration

Section name: **`UsageLicense`** in `appsettings.json` / environment-specific files (`appsettings.Development.json`, `appsettings.Production.json`, Docker-mounted config, IIS `appsettings.Production.json`).

```json
"UsageLicense": {
  "Enabled": false,
  "TrialDays": 90,
  "TrialStartUtc": null,
  "TrialEndUtc": null,
  "ContactEmail": "",
  "ContactUrl": ""
}
```

| Property | Type | Description |
|----------|------|-------------|
| `Enabled` | `bool` | Master switch. `false` = no banner. |
| `TrialEndUtc` | `DateTime?` | **Preferred.** UTC calendar date when the trial ends (date part only; time ignored). |
| `TrialStartUtc` | `DateTime?` | UTC start date. End = start + `TrialDays` when `TrialEndUtc` is not set. |
| `TrialDays` | `int` | Length of trial when using `TrialStartUtc`. Default `90`. |
| `ContactEmail` | `string?` | Reserved for future UI (not rendered today). |
| `ContactUrl` | `string?` | Reserved for future UI (not rendered today). |

### End date resolution

1. If `TrialEndUtc` is set → use that date (UTC, date-only).
2. Else if `TrialStartUtc` is set → `TrialStartUtc + TrialDays`.
3. Else → banner is **not** shown (even if `Enabled` is `true`).

**Days remaining** = `(endDate - UtcToday).Days`, clamped to `0` minimum. When `0`, the **expired** title string is used and the `--expired` CSS modifier is applied.

### Examples

**Production trial until a fixed date:**

```json
"UsageLicense": {
  "Enabled": true,
  "TrialEndUtc": "2026-07-31",
  "TrialDays": 90
}
```

**Trial from install date (90 days):**

```json
"UsageLicense": {
  "Enabled": true,
  "TrialStartUtc": "2026-05-01",
  "TrialDays": 90
}
```

**Fully licensed customer (no banner):**

```json
"UsageLicense": {
  "Enabled": false
}
```

**Local dev preview** (`appsettings.Development.json` in repo — adjust dates as needed):

```json
"UsageLicense": {
  "Enabled": true,
  "TrialStartUtc": "2026-05-26",
  "TrialDays": 90
}
```

### Docker / on-prem

Set the same `UsageLicense` block in the environment’s production settings (e.g. `.env.prod` does not define this directly — use `appsettings.Production.json` in the published host or compose volume mount). Per-customer trial dates are typically set at deploy time, not in source control.

---

## Localization

Strings are **Layer A** UI messages in:

- Source: `tools/GenerateModelLocalization/UiStrings.messages.json`
- Keys:
  - `UsageLicense.Banner.Title` — `{0}` = days remaining (active trial)
  - `UsageLicense.Banner.TitleExpired` — no placeholders (expired)
- Runtime: `VisaUiMessages` / `VisaUiMessageCatalog.g.cs`

After editing `UiStrings.messages.json`, regenerate the catalog:

```powershell
dotnet run --project tools/GenerateModelLocalization/GenerateModelLocalization.csproj
```

Supported cultures: `en-US`, `tr-TR`, `tk-TM`, `ru-RU` (same as the rest of the app). The login page uses `CultureInfo.CurrentUICulture` when `_Host.cshtml` is rendered.

---

## Implementation map

| File | Role |
|------|------|
| `Visa2026.Blazor.Server/UsageLicenseOptions.cs` | Configuration model (`UsageLicense` section). |
| `Visa2026.Blazor.Server/UsageLicensePresenter.cs` | Builds `UsageLicenseBannerViewModel` from config + dates + localized title. |
| `Visa2026.Blazor.Server/Pages/_Host.cshtml` | Renders banner HTML; login-only visibility script (with version badge). |
| `Visa2026.Blazor.Server/wwwroot/css/site.css` | `.usage-license-banner` styles. |
| `Visa2026.Blazor.Server/appsettings.json` | Default: `Enabled: false`. |

Logic stays in the **Blazor host** only — do not move to `Visa2026.Module` unless you need the same rules inside XAF controllers or API.

---

## Customization

### Copy / wording

Edit `UsageLicense.Banner.*` in `UiStrings.messages.json`, run the localization tool (above), rebuild.

### Look and feel

Edit `.usage-license-banner` rules in `site.css`. CSS variables on the root class control accent, text, background, and border:

```css
.usage-license-banner {
    --usage-license-accent: #c2410c;
    --usage-license-fg: #7c2d12;
    --usage-license-bg: #fffdfb;
    --usage-license-border: #ede4d8;
}
```

### Show on other routes

Today only `/LoginPage` is targeted. To extend visibility, update the `isLoginRoute()` check in `_Host.cshtml` (same script block as `app-version-badge`).

---

## Troubleshooting

| Symptom | Likely cause |
|---------|----------------|
| No banner on login | `Enabled: false`, or missing `TrialEndUtc` / `TrialStartUtc`, or wrong environment file loaded. |
| Always expired (`0` days) | `TrialEndUtc` or `TrialStartUtc + TrialDays` is today or in the past (UTC). |
| Old text after JSON edit | Regenerate `VisaUiMessageCatalog.g.cs` and restart the app. |
| Styles unchanged | Hard refresh (`Ctrl+F5`) — `site.css` is cache-busted with `asp-append-version`. |
| Banner after login | Should not happen; check `isLoginRoute()` / Blazor navigation hooks in `_Host.cshtml`. |

---

## Design notes (for future changes)

- **Notice tier, not error tier:** Soft amber/warm neutral bar — visible before login, does not compete with the primary **Giriş** button.
- **Single line:** Avoid long legal text on the login screen; details belong in contract / admin comms.
- **Config-driven dates:** No database table; trial windows are set per deployment / customer in appsettings.
- **Not a security boundary:** Treat as UX transparency only unless you add server-side checks elsewhere.

---

## Related docs

- [`ENVIRONMENTS.md`](ENVIRONMENTS.md) — Docker and environment-specific configuration.
- [`ON_PREM_WINDOWS_IIS.md`](ON_PREM_WINDOWS_IIS.md) — IIS deploy; set `appsettings.Production.json` on the server.
- [`dx-watermark-suppression.md`](dx-watermark-suppression.md) — DevExpress evaluation watermark (separate concern).
