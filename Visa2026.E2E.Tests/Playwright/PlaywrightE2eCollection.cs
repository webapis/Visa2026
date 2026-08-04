using Xunit;

namespace Visa2026.E2E.Tests.Playwright;

[CollectionDefinition(Name)]
public class PlaywrightE2eCollection : ICollectionFixture<PlaywrightE2eFixture>
{
    public const string Name = "PlaywrightE2E";
}
