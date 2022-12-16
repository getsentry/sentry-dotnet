namespace Sentry.Android;

internal static class AndroidHelpers
{
    public static string? GetCpuAbi() => GetSupportedAbis().FirstOrDefault();

    public static IList<string> GetSupportedAbis()
    {
        var result = AndroidBuild.SupportedAbis;
        if (result != null)
        {
            return result;
        }

#pragma warning disable CS0618
        var abi = AndroidBuild.CpuAbi;
#pragma warning restore CS0618

        return abi != null ? new[] {abi} : Array.Empty<string>();
    }
}
