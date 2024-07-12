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
#pragma warning disable CS0618 // Type or member is obsolete
            Segment = user.Segment,
#pragma warning restore CS0618 // Type or member is obsolete
            Other = user.Data ?? EmptyDictionary
        };

    public static JavaSdk.Protocol.User ToJavaUser(this SentryUser user) =>
        new()
        {
            Email = user.Email,
            Id = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
#pragma warning disable CS0618 // Type or member is obsolete
            Segment = user.Segment,
#pragma warning restore CS0618 // Type or member is obsolete
            Data = user.Other.Count == 0 ? null : user.Other
        };
}
