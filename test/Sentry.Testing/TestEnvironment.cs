namespace Sentry.Testing;

public static class TestEnvironment
{
    public static bool IsGitHubActions
    {
        get
        {
            var isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            return isGitHubActions?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        }
    }

    /// <summary>
    /// See https://docs.github.com/en/actions/writing-workflows/choosing-what-your-workflow-does/store-information-in-variables#default-environment-variables
    /// </summary>
    public static bool IsCI
    {
        get
        {
            var isGitHubActions = Environment.GetEnvironmentVariable("CI");
            return isGitHubActions?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
