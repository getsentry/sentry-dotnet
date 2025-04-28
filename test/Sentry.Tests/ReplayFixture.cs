namespace Sentry.Tests;

public class ReplayFixture
{
    public ReplayFixture()
    {
        ReplayHelper.InitTestReplayId();
    }
}

/// <summary>
/// This class has no code, and is never created. Its purpose is simply to be the place to apply [CollectionDefinition]
/// and the <see cref="ICollectionFixture{TFixture}"/> interface.
/// </summary>
[CollectionDefinition("Replay collection")]
public class ReplayCollection : ICollectionFixture<ReplayFixture>
{
    // TODO: When we upgrade to Xcode 3, it would be cleaner to use an AssemblyFixture
}
