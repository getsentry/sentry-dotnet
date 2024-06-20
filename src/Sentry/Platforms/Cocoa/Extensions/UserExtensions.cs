using Sentry.Extensibility;

namespace Sentry.Cocoa.Extensions;

internal static class UserExtensions
{
    public static SentryUser ToUser(this CocoaSdk.SentryUser user, IDiagnosticLogger? logger = null) =>
        new()
        {
            Email = user.Email,
            Id = user.UserId,
            IpAddress = user.IpAddress,
            Username = user.Username,
#pragma warning disable CS0618 // Type or member is obsolete
            Segment = user.Segment,
#pragma warning restore CS0618 // Type or member is obsolete
            Other = user.Data.ToStringDictionary(logger)
        };

    public static CocoaSdk.SentryUser ToCocoaUser(this SentryUser user)
    {
        var cocoaUser = new CocoaSdk.SentryUser
        {
            Email = user.Email,
            UserId = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
#pragma warning disable CS0618 // Type or member is obsolete
            Segment = user.Segment,
#pragma warning restore CS0618 // Type or member is obsolete
            Data = user.Other.ToNullableNSDictionary()
        };

        return cocoaUser;
    }
}
