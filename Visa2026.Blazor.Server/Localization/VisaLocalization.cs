using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Visa2026.Blazor.Server.Localization;

/// <summary>
/// Layer A (UI) cultures for ASP.NET Core request localization and XAF Application Model.
/// First entry is the default (<see cref="DefaultCultureName"/>).
/// </summary>
public static class VisaLocalization
{
    public const string DefaultCultureName = "en-US";

    /// <summary>Semicolon-separated list for <c>DevExpress:ExpressApp:Languages</c> in appsettings.</summary>
    public static string LanguagesConfigurationValue => string.Join(";", SupportedCultureNames);

    public static IReadOnlyList<string> SupportedCultureNames { get; } = new[]
    {
        DefaultCultureName,
        "tr-TR",
        "tk-TM",
        "ru-RU",
    };

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddLocalization();

        var cultures = CreateSupportedCultures();
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(DefaultCultureName);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            // Cookie (used by IXafCultureInfoService in A2+) and query string for manual testing.
            // Accept-Language is omitted so the default stays English until the user picks a language.
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new CookieRequestCultureProvider(),
                new QueryStringRequestCultureProvider(),
            };
        });
    }

    public static void UseVisaRequestLocalization(IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(options);
        app.UseMiddleware<VisaCulturePersistenceMiddleware>();
    }

    public static bool TryNormalizeCulture(string? cultureName, out string normalizedName)
    {
        normalizedName = DefaultCultureName;
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return false;
        }

        foreach (var supported in SupportedCultureNames)
        {
            if (string.Equals(cultureName, supported, StringComparison.OrdinalIgnoreCase))
            {
                normalizedName = supported;
                return true;
            }
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            foreach (var supported in SupportedCultureNames)
            {
                var supportedCulture = CultureInfo.GetCultureInfo(supported);
                if (culture.Equals(supportedCulture)
                    || culture.TwoLetterISOLanguageName.Equals(
                        supportedCulture.TwoLetterISOLanguageName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    normalizedName = supported;
                    return true;
                }
            }
        }
        catch (CultureNotFoundException)
        {
            // fall through
        }

        return false;
    }

    /// <summary>Application title for HTML &lt;title&gt;, splash screen, and pre-Blazor host shell.</summary>
    public static string GetApplicationTitle()
    {
        if (!TryNormalizeCulture(CultureInfo.CurrentUICulture.Name, out string culture))
        {
            return "Visa Management";
        }

        return culture switch
        {
            "tr-TR" => "Vize Yönetimi",
            "tk-TM" => "Wiza dolandyryşy",
            "ru-RU" => "Управление визами",
            _ => "Visa Management",
        };
    }

    /// <summary>DevExpress Blazor grid search box placeholder (not in XAF Application Model).</summary>
    public static string GetGridSearchBoxNullText() =>
        GetShellText("TypeToSearch", GetShellText("TextToSearch", "Text to search..."));

    /// <summary>DevExpress Blazor grid empty-state message (not in XAF Application Model).</summary>
    public static string GetGridEmptyDataText() => GetShellText("NoDataToDisplay", "No data to display");

    private static string GetShellText(string key, string englishFallback)
    {
        if (!TryNormalizeCulture(CultureInfo.CurrentUICulture.Name, out string culture))
        {
            return englishFallback;
        }

        return culture switch
        {
            "tr-TR" => key switch
            {
                "TextToSearch" => "Aranacak metin...",
                "TypeToSearch" => "Aranacak metin yazın...",
                "NoDataToDisplay" => "Gösterilecek veri yok",
                _ => englishFallback,
            },
            "tk-TM" => key switch
            {
                "TextToSearch" => "Gözleýän tekst...",
                "TypeToSearch" => "Gözlemek üçin ýazyň...",
                "NoDataToDisplay" => "Görkezmek üçin maglumat ýok",
                _ => englishFallback,
            },
            "ru-RU" => key switch
            {
                "TextToSearch" => "Текст для поиска...",
                "TypeToSearch" => "Введите текст для поиска...",
                "NoDataToDisplay" => "Нет данных для отображения",
                _ => englishFallback,
            },
            _ => englishFallback,
        };
    }

    private static IList<CultureInfo> CreateSupportedCultures()
    {
        var list = new List<CultureInfo>();
        foreach (var name in SupportedCultureNames)
        {
            try
            {
                list.Add(CultureInfo.GetCultureInfo(name));
            }
            catch (CultureNotFoundException) when (name == "tk-TM")
            {
                // Some runtimes omit tk-TM; neutral Turkmen is enough for UI formatting in A1.
                list.Add(CultureInfo.GetCultureInfo("tk"));
            }
        }

        return list;
    }
}
