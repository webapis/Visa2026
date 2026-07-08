using DevExpress.ExpressApp.Blazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Visa2026.Blazor.Server.Components;

/// <summary>
/// Full-screen Calik splash with watermark logo, loading progress, and copyright footer.
/// </summary>
public sealed class CalikSplashScreen : ComponentBase
{
    private const string DefaultLogoPath = "images/CalikLogo.png";

    [CascadingParameter(Name = "ImagePath")]
    public string? ImagePath { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<SplashScreenComponent>(0);
        builder.AddAttribute(1, "LoadingIndicator", (RenderFragment)(RenderSplash));
        builder.CloseComponent();
    }

    private void RenderSplash(RenderTreeBuilder builder)
    {
        var logoPath = string.IsNullOrWhiteSpace(ImagePath) ? DefaultLogoPath : ImagePath;
        var copyrightYear = DateTime.UtcNow.Year;

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "visa-splash-screen");

        builder.OpenElement(2, "img");
        builder.AddAttribute(3, "class", "visa-splash-screen__watermark");
        builder.AddAttribute(4, "src", logoPath);
        builder.AddAttribute(5, "alt", "");
        builder.AddAttribute(6, "aria-hidden", "true");
        builder.CloseElement();

        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "visa-splash-screen__center");
        builder.OpenElement(9, "img");
        builder.AddAttribute(10, "class", "visa-splash-screen__logo");
        builder.AddAttribute(11, "src", logoPath);
        builder.AddAttribute(12, "alt", "");
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(13, "div");
        builder.AddAttribute(14, "class", "visa-splash-screen__load");
        builder.OpenElement(15, "div");
        builder.AddAttribute(16, "class", "visa-splash-screen__load-label");
        builder.AddContent(17, "Loading App Data...");
        builder.CloseElement();
        builder.OpenElement(18, "div");
        builder.AddAttribute(19, "class", "visa-splash-screen__load-row");
        builder.OpenElement(20, "div");
        builder.AddAttribute(21, "class", "visa-splash-screen__progress");
        builder.AddAttribute(22, "role", "progressbar");
        builder.AddAttribute(23, "aria-label", "Loading");
        builder.AddAttribute(24, "aria-valuemin", "0");
        builder.AddAttribute(25, "aria-valuemax", "100");
        builder.AddAttribute(26, "aria-valuenow", "0");
        builder.OpenElement(27, "div");
        builder.AddAttribute(28, "class", "visa-splash-screen__progress-bar");
        builder.AddAttribute(29, "id", "visaSplashProgressBar");
        builder.CloseElement();
        builder.CloseElement();
        builder.OpenElement(30, "span");
        builder.AddAttribute(31, "class", "visa-splash-screen__percent");
        builder.AddAttribute(32, "id", "visaSplashProgressPercent");
        builder.AddContent(33, "0%");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(34, "div");
        builder.AddAttribute(35, "class", "visa-splash-screen__copyright");
        builder.AddContent(36, $"© {copyrightYear} ÇALIK Group");
        builder.CloseElement();

        builder.CloseElement();
    }
}