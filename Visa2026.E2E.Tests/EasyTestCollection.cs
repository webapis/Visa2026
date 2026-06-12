using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Visa2026.E2E.Tests;

[CollectionDefinition(Name)]
public class EasyTestCollection : ICollectionFixture<EasyTestSessionFixture>
{
    public const string Name = "EasyTest";
}
