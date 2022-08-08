using Sentry.Extensibility;

namespace Sentry.iOS.Extensions;

internal static class UserExtensions
{
    public static User ToUser(this SentryCocoa.SentryUser user, IDiagnosticLogger? logger = null) =>
        new()
        {
            Email = user.Email,
            Id = user.UserId,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Other = user.Data.ToStringDictionary(logger)
        };

    public static SentryCocoa.SentryUser ToCocoaUser(this User user)
    {
        var cocoaUser = new SentryCocoa.SentryUser
        {
            Email = user.Email,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Data = user.Other.ToNullableNSDictionary()
        };

        // Leave a null User ID uninitialized since it is optional.
        // It should be nullable in the Sentry Cocoa SDK, but isn't currently.
        // See: https://github.com/getsentry/sentry-cocoa/issues/2035
        if (user.Id != null)
        {
            cocoaUser.UserId = user.Id;
        }

        return cocoaUser;
    }
}
