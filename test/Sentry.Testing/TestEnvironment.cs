namespace Sentry.Testing;

public static class TestEnvironment
{
    public static bool IsGitHubActions
    {
        get
        {
            // Checking if the GITHUB_ACTIONS environment variable is set to true
            var isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
            return isGitHubActions?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
