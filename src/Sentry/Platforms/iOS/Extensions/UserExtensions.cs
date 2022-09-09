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
            UserId = user.Id,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Data = user.Other.ToNullableNSDictionary()
        };

        return cocoaUser;
    }
}
