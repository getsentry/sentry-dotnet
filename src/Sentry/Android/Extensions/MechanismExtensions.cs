using Sentry.Protocol;

namespace Sentry.Android.Extensions;

internal static class MechanismExtensions
{
    public static Mechanism ToMechanism(this Java.Protocol.Mechanism mechanism)
    {
        var result = new Mechanism
        {
            Type = mechanism.Type,
            Description = mechanism.Description,
            HelpLink = mechanism.HelpLink,
            Handled = mechanism.IsHandled()?.BooleanValue()
        };

        if (mechanism.Meta is { } meta)
        {
            foreach (var item in meta)
            {
                result.Meta.Add(item.Key, item.Value);
            }
        }

        if (mechanism.Data is { } data)
        {
            foreach (var item in data)
            {
                result.Data.Add(item.Key, item.Value);
            }
        }

        return result;
    }

    public static Java.Protocol.Mechanism ToJavaMechanism(this Mechanism mechanism)
    {
        var result = new Java.Protocol.Mechanism
        {
            Type = mechanism.Type,
            Description = mechanism.Description,
            HelpLink = mechanism.HelpLink,
            Meta = mechanism.Meta.ToDictionary(x => x.Key, x => (JavaObject)x.Value),
            Data = mechanism.Data.ToDictionary(x => x.Key, x => (JavaObject)x.Value),
        };

        if (mechanism.Handled is { } handled)
        {
            result.SetHandled(new JavaBoolean(handled));
        }

        return result;
    }
}
