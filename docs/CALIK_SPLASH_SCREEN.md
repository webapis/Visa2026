# Calik splash screen and application logo

Host-only branding shown while the XAF Blazor app starts (before the main shell is interactive). Replaces the default DevExpress splash (small circular badge + caption) with a full-screen **Calik Holding** loading experience.

**Not** the login-page usage-license banner — see [`USAGE_LICENSE_LOGIN_BANNER.md`](USAGE_LICENSE_LOGIN_BANNER.md).

DevExpress reference: [Application Icon, Logo & About Info](https://docs.devexpress.com/eXpressAppFramework/113445), [Change an Application Logo and Info](https://docs.devexpress.com/eXpressAppFramework/113156).

---

## What users see

| Area | Behavior |
|------|----------|
| **Splash (startup)** | Full viewport, light gray background; faded watermark logo; main logo centered; "Loading App Data..." with gradient progress bar and **%**; `(c) {year} CALIK Group` bottom-right. |
| **Header (after login)** | Color Calik logo top-left via `.header-logo` in `site.css` (not the monochromatic SVG mask default). |
| **Browser tab title** | Unchanged — still `VisaLocalization.GetApplicationTitle()` (e.g. localized "Wiza dolandyrysy"). Splash caption is intentionally empty. |

The splash is removed when XAF adds `loading-hide` to `#applicationLoadingPanel` (same hook as the stock splash).

---

## Architecture

1. **`_Host.cshtml`** registers the stock `SplashScreen` component with:
   - `param-Caption='""'` — hide application name on splash
   - `param-ImagePath='"images/CalikLogo.png"'`
   - `param-ContentType="@typeof(CalikSplashScreen)"` — custom inner markup
2. **`CalikSplashScreen`** (`ComponentBase`, not `.razor`) builds the splash DOM inside `SplashScreenComponent` -> `LoadingIndicator`.
3. **`site.css`** overrides XAF inline splash dimensions (default logo was ~46x30px inside a 120px circle).
4. **Inline script** in `_Host.cshtml` animates progress 0% -> 92% over ~10s, then **100%** when `loading-hide` appears.

---

## File map

| File | Role |
|------|------|
| `Visa2026.Blazor.Server/Components/CalikSplashScreen.cs` | Splash HTML structure (watermark, logo, progress, copyright). |
| `Visa2026.Blazor.Server/Pages/_Host.cshtml` | Wires `SplashScreen` + progress script. |
| `Visa2026.Blazor.Server/wwwroot/css/site.css` | `#applicationLoadingPanel` + `.visa-splash-screen*` + `.header-logo`. |
| `Visa2026.Blazor.Server/wwwroot/images/CalikLogo.png` | Web asset for splash and header. |
| `Visa2026.Module/Images/CalikLogo.png` | Embedded resource for XAF `Application.Logo`. |
| `Visa2026.Module/Visa2026.Module.csproj` | `<EmbeddedResource Include="Images\CalikLogo.png" />` |
| `Visa2026.Blazor.Server/Model.xafml` | `<Application Logo="CalikLogo" ...>` (omit extension per XAF image picker rules). |

---

## DOM structure and CSS classes

Rendered inside `#applicationLoadingPanel`:

- `.visa-splash-screen` — full-viewport root
- `.visa-splash-screen__watermark` — faded background logo
- `.visa-splash-screen__center` / `__logo` — main logo
- `.visa-splash-screen__load` — bottom progress block
- `.visa-splash-screen__progress-bar` — `#visaSplashProgressBar`
- `.visa-splash-screen__percent` — `#visaSplashProgressPercent`
- `.visa-splash-screen__copyright` — footer line

All splash layout rules are scoped under `#applicationLoadingPanel` so they override XAF inline `<style>` in `<body>`.

---

## Customization

### Replace the logo

1. Swap `Visa2026.Blazor.Server/wwwroot/images/CalikLogo.png` (or update `DefaultLogoPath` in `CalikSplashScreen.cs`, `_Host.cshtml` `ImagePath`, and `.header-logo` in `site.css`).
2. Replace `Visa2026.Module/Images/CalikLogo.png` when keeping `Logo="CalikLogo"` in `Model.xafml`.
3. Rebuild; hard-refresh (`Ctrl+F5`) after deploy.

### Loading label / copyright

Edit strings in `CalikSplashScreen.cs` (`RenderSplash`). Copyright year uses `DateTime.UtcNow.Year`.

### Progress timing (`_Host.cshtml` script)

| Variable | Default | Effect |
|----------|---------|--------|
| `durationMs` | `10000` | Time to reach ~92% |
| `target` | `92` | Max simulated % before app ready |

On `loading-hide`, the bar jumps to **100%**.

### Visual design

Edit `site.css` under `/* Custom Calik splash`. Header: `.header-logo` (default 132x38px).

---

## Implementation notes

### Why C# (`ComponentBase`), not `.razor`

Razor codegen for the splash child caused escape-sequence build errors. `BuildRenderTree` matches the DevExpress custom splash pattern and is stable.

### File encoding

Save `CalikSplashScreen.cs` as **UTF-8 (no BOM)**. UTF-16 produces `CS1056 Unexpected character '\0'`. Re-save with UTF-8 if the IDE shows nonsense errors on line 1.

### XAF defaults disabled (CSS)

- `.loading-caption` hidden
- `.loading-border`, `.loading-floated-circle` hidden (spinner rings)

---

## Verify after changes

1. `dotnet build Visa2026.slnx -c Release`
2. Run host; confirm watermark, logo, progress %, copyright on startup
3. Confirm header Calik logo after login
4. View source on `/` for `visa-splash-screen` / `CalikLogo.png`

---

## Related host chrome

| Feature | Doc |
|---------|-----|
| Login usage-license banner | [`USAGE_LICENSE_LOGIN_BANNER.md`](USAGE_LICENSE_LOGIN_BANNER.md) |
| DevExpress license DOM suppression | [`dx-watermark-suppression.md`](dx-watermark-suppression.md) |
| Version badge on login | `_Host.cshtml` (`app-version-badge`) |