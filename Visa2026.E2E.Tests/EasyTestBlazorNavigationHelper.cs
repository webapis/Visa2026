using System.Linq;
using System.Reflection;
using DevExpress.EasyTest.Framework;
using OpenQA.Selenium;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Blazor view navigation via Selenium URL (e.g. <c>/Person_ListView_Employees</c>).
/// EasyTest <see cref="ApplicationContextExtensions.Navigate"/> does not reliably activate the correct TabbedMDI tab.
/// </summary>
internal static class EasyTestBlazorNavigationHelper
{
    public static void GoToRelativeUrl(IApplicationContext appContext, string baseUrl, string relativePath)
    {
        IWebDriver driver = ResolveWebDriver(appContext)
            ?? throw new InvalidOperationException("Could not resolve Selenium IWebDriver from EasyTest context.");

        string url = $"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        driver.Navigate().GoToUrl(url);
        WaitForDocumentReady(driver, TimeSpan.FromSeconds(45));
    }

    /// <summary>
    /// Best-effort capture of the current browser state (URL, page HTML, screenshot)
    /// into <paramref name="outputDirectory"/> for post-mortem of CI-only failures.
    /// Never throws — diagnostics must not mask the original test error.
    /// </summary>
    public static void TryDumpDiagnostics(IApplicationContext appContext, string outputDirectory, string label)
    {
        IWebDriver? driver = ResolveWebDriver(appContext);
        if (driver == null)
            return;

        string baseName;
        try
        {
            Directory.CreateDirectory(outputDirectory);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            baseName = Path.Combine(outputDirectory, $"diag-{label}-{stamp}");
        }
        catch
        {
            return;
        }

        try { File.WriteAllText(baseName + ".url.txt", driver.Url ?? string.Empty); }
        catch { /* diagnostics are best-effort */ }

        try { File.WriteAllText(baseName + ".page.html", driver.PageSource ?? string.Empty); }
        catch { /* diagnostics are best-effort */ }

        try
        {
            if (driver is ITakesScreenshot shooter)
                shooter.GetScreenshot().SaveAsFile(baseName + ".png");
        }
        catch { /* diagnostics are best-effort */ }
    }

    public static bool UrlContains(IApplicationContext appContext, string fragment) =>
        TryGetCurrentUrl(appContext, out string? url)
        && url.Contains(fragment, StringComparison.OrdinalIgnoreCase);

    public static string GetCurrentUrl(IApplicationContext appContext) =>
        TryGetCurrentUrl(appContext, out string? url) ? url : string.Empty;

    public static bool ListHasColumnHeader(IApplicationContext appContext, string columnCaption)
    {
        IWebDriver? driver = ResolveWebDriver(appContext);
        if (driver == null)
            return false;

        try
        {
            string xpath =
                $"//table[contains(@class,'dxbl-grid')]//th[contains(normalize-space(.), '{columnCaption}')]";
            return driver.FindElements(By.XPath(xpath)).Any(e => e.Displayed);
        }
        catch (WebDriverException)
        {
            return false;
        }
    }

    public static bool TryExecuteJavaScript(IApplicationContext appContext, string script, out object? result)
    {
        result = null;
        if (ResolveWebDriver(appContext) is not IJavaScriptExecutor js)
            return false;

        try
        {
            result = js.ExecuteScript(script);
            return true;
        }
        catch (WebDriverException)
        {
            return false;
        }
    }

    /// <summary>
    /// Native-DOM click of a DevExpress toolbar action button by its rendered
    /// <c>title</c> prefix (e.g. a nested list's <c>New Passport</c>). XAF renders an
    /// empty <c>data-xaf-action</c> on nested ListPropertyEditor actions and, on small
    /// headed CI viewports, an adaptive <c>dxbl-virtual-el</c> measurement clone, so
    /// EasyTest's <c>GetAction("New").Execute()</c> is ambiguous between sibling nested
    /// grids and can no-op. Clicking the real, displayed button is deterministic.
    /// Mirrors the existing <see cref="ClickListRowContaining"/> DOM helper.
    /// </summary>
    public static bool TryClickToolbarActionByTitle(IApplicationContext appContext, string titlePrefix, TimeSpan timeout)
    {
        IWebDriver? driver = ResolveWebDriver(appContext);
        if (driver == null)
            return false;

        string literal = titlePrefix.Replace("'", "\\'");
        string xpath =
            $"//button[@data-action-name and starts-with(@title, '{literal}') and not(@dxbl-virtual-el)]";

        DateTime deadline = DateTime.UtcNow + timeout;
        do
        {
            try
            {
                foreach (IWebElement button in driver.FindElements(By.XPath(xpath)))
                {
                    if (!button.Displayed || !button.Enabled)
                        continue;

                    ScrollIntoView(driver, button);
                    button.Click();
                    return true;
                }
            }
            catch (WebDriverException)
            {
                // Re-query after a Blazor re-render.
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(300));
        }
        while (DateTime.UtcNow < deadline);

        return false;
    }

    /// <summary>
    /// Maximizes the browser window so DevExpress adaptive toolbars keep their actions
    /// inline (collapsed toolbars hide nested-list <c>New</c> behind virtual clones on
    /// small CI viewports). Best-effort — never throws.
    /// </summary>
    public static void TryMaximizeWindow(IApplicationContext appContext)
    {
        IWebDriver? driver = ResolveWebDriver(appContext);
        if (driver == null)
            return;

        try
        {
            driver.Manage().Window.Maximize();
        }
        catch (WebDriverException)
        {
            // Headless / unsupported window manager — ignore.
        }
    }

    private static void ScrollIntoView(IWebDriver driver, IWebElement element)
    {
        try
        {
            if (driver is IJavaScriptExecutor js)
                js.ExecuteScript("arguments[0].scrollIntoView({block:'center'});", element);
        }
        catch (WebDriverException)
        {
            // Non-fatal — Click still attempts its own scroll.
        }
    }

    public static void ClickListRowContaining(IApplicationContext appContext, string cellText)
    {
        IWebDriver driver = ResolveWebDriver(appContext)
            ?? throw new InvalidOperationException("Could not resolve Selenium IWebDriver from EasyTest context.");

        string literal = cellText.Replace("'", "\\'");
        string xpath =
            $"//table[contains(@class,'dxbl-grid')]//tr[contains(@class,'dxbl-grid-data-row') and contains(., '{literal}')]";

        Exception? lastError = null;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            try
            {
                foreach (IWebElement row in driver.FindElements(By.XPath(xpath)))
                {
                    if (!row.Displayed)
                        continue;

                    row.Click();
                    Thread.Sleep(TimeSpan.FromMilliseconds(500));
                    return;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(500));
        }

        throw new InvalidOperationException(
            $"List row containing '{cellText}' was not found.",
            lastError);
    }

    private static bool TryGetCurrentUrl(IApplicationContext appContext, out string url)
    {
        url = ResolveWebDriver(appContext)?.Url ?? string.Empty;
        return !string.IsNullOrEmpty(url);
    }

    private static void WaitForDocumentReady(IWebDriver driver, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (driver is IJavaScriptExecutor js)
                {
                    string? state = js.ExecuteScript("return document.readyState") as string;
                    if (state == "complete")
                        return;
                }
            }
            catch (WebDriverException)
            {
                // Blazor circuit reconnect — keep polling.
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(250));
        }
    }

    private static IWebDriver? ResolveWebDriver(IApplicationContext appContext)
    {
        PropertyInfo? adapterProperty = appContext.GetType().GetProperty(
            "CommandAdapter",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        object? adapter = adapterProperty?.GetValue(appContext);
        return adapter == null ? null : FindWebDriver(adapter);
    }

    private static IWebDriver? FindWebDriver(object instance, int depth = 0)
    {
        if (depth > 5)
            return null;

        if (instance is IWebDriver driver)
            return driver;

        Type type = instance.GetType();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            object? value;
            try
            {
                value = property.GetValue(instance);
            }
            catch
            {
                continue;
            }

            if (value is IWebDriver nestedDriver)
                return nestedDriver;

            if (value != null && !value.GetType().IsValueType)
            {
                IWebDriver? found = FindWebDriver(value, depth + 1);
                if (found != null)
                    return found;
            }
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            object? value;
            try
            {
                value = field.GetValue(instance);
            }
            catch
            {
                continue;
            }

            if (value is IWebDriver nestedDriver)
                return nestedDriver;

            if (value != null && !value.GetType().IsValueType)
            {
                IWebDriver? found = FindWebDriver(value, depth + 1);
                if (found != null)
                    return found;
            }
        }

        return null;
    }
}
