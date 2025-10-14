namespace NQueen.UnitTests.Fixtures;

// Shared collection so all solver tests reuse a single fixture instance (one DI container)
[CollectionDefinition("SolverBackend")]
public class SolverBackendCollection : ICollectionFixture<SolverBackEndFixture> { }
