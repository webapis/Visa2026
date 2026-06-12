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

    internal static IWebDriver? TryGetWebDriver(IApplicationContext appContext) =>
        ResolveWebDriver(appContext);

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
