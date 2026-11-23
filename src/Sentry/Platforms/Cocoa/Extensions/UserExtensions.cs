namespace Sentry.Cocoa.Extensions;

internal static class UserExtensions
{
    public static CocoaSdk.SentryObjCUser ToCocoaUser(this SentryUser user)
    {
        var cocoaUser = new CocoaSdk.SentryObjCUser
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
