namespace Sentry.Android.Extensions;

internal static class UserExtensions
{
    public static JavaSdk.Protocol.User ToJavaUser(this SentryUser user) =>
        new()
        {
            Email = user.Email,
            Id = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Data = user.Other.Count == 0 ? null : user.Other
        };
}
