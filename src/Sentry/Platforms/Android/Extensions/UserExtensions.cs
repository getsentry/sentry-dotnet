using System.Collections.ObjectModel;

namespace Sentry.Android.Extensions;

internal static class UserExtensions
{
    private static readonly IDictionary<string, string> EmptyDictionary =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    public static User ToUser(this JavaSdk.Protocol.User user) =>
        new()
        {
            Email = user.Email,
            Id = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Other = user.Others ?? EmptyDictionary
        };

    public static JavaSdk.Protocol.User ToJavaUser(this User user) =>
        new()
        {
            Email = user.Email,
            Id = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Others = user.Other.Count == 0 ? null : user.Other
        };
}
