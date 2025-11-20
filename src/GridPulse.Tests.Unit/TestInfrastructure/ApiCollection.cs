using Xunit;

namespace GridPulse.Tests.Unit.TestInfrastructure;

[CollectionDefinition(TestCollections.Api)]
public sealed class ApiCollection : ICollectionFixture<GridPulseApiFactory>
{
}