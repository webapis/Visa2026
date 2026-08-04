namespace Visa2026.E2E.Tests.Playwright;

/// <summary>Where Playwright E2E runs — same journeys, different host.</summary>
public enum PlaywrightE2eTarget
{
    /// <summary>Local isolated host (:5050) with fresh <c>visa2026_easytest</c> DB.</summary>
    Local,

    /// <summary>Live staging IIS/Docker URL — no DB reset, manual trigger.</summary>
    Staging,
}
