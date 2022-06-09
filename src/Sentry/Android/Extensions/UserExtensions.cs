namespace Sentry.Android.Extensions;

internal static class UserExtensions
{
    public static User ToUser(this Java.Protocol.User user) =>
        new()
        {
            Email = user.Email,
            Id = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Other = user.Others ?? new Dictionary<string, string>()
        };

    public static Java.Protocol.User ToJavaUser(this User user) =>
        new()
        {
            Email = user.Email,
            Id = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Others = user.Other.Count == 0 ? null : user.Other
        };
}
