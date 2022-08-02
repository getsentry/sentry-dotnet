using System.Collections.ObjectModel;
using System.Text.Json;

namespace Sentry.iOS.Extensions;

internal static class UserExtensions
{
    private static readonly IDictionary<string, string> EmptyDictionary =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    public static User ToUser(this SentryCocoa.SentryUser user)
    {
        IDictionary<string, string>? otherData;
        if (user.Data is not IDictionary<NSString, NSObject> data)
        {
            otherData = EmptyDictionary;
        }
        else
        {
            otherData = data
                .ToDictionary(
                    x => (string)x.Key,
                    x =>
                    {
                        try
                        {
                            return JsonSerializer.Serialize(x.Value);
                        }
                        catch
                        {
                            // TODO: log error
                            return "";
                        }
                    });
        }

        return new User
        {
            Email = user.Email,
            Id = user.UserId,
            IpAddress = user.IpAddress,
            Username = user.Username,
            Other = otherData
        };
    }

    public static SentryCocoa.SentryUser ToCocoaUser(this User user)
    {
        NSDictionary<NSString, NSObject>? userData;
        if (user.Other.Count == 0)
        {
            userData = null;
        }
        else
        {
            userData = new NSDictionary<NSString, NSObject>();
            foreach (var item in user.Other)
            {
                userData[item.Key] = NSObject.FromObject(item.Value);
            }
        }

        return new SentryCocoa.SentryUser
        {
            Email = user.Email,
            UserId = user.Id ?? "",
            IpAddress = user.IpAddress,
            Username = user.Username,
            Data = userData
        };
    }
}
