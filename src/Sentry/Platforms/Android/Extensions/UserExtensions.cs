namespace Sentry.Android.Extensions;

internal static class UserExtensions
{
    private static readonly IDictionary<string, string> EmptyDictionary =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    public static SentryUser ToUser(this JavaSdk.Protocol.User user) =>
        new()
        {
            Email = user.Email,
            Id = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Segment = user.Segment,
            Other = user.Data ?? EmptyDictionary
        };

    public static JavaSdk.Protocol.User ToJavaUser(this SentryUser user) =>
        new()
        {
            Email = user.Email,
            Id = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Segment = user.Segment,
            Data = user.Other.Count == 0 ? null : user.Other
        };
}
