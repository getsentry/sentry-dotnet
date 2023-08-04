namespace Sentry.Testing;

public static class StringExtensions
{
    private static readonly string PathSeparator = Path.DirectorySeparatorChar.ToString();

    public static string OsAgnostic(this string path) => path.Replace("/", PathSeparator);
    public static string TrimLeadingPathSeparator(this string path) => path[..1] == PathSeparator ? path[1..] : path;
}
