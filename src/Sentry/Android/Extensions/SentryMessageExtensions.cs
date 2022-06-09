namespace Sentry.Android.Extensions;

internal static class SentryMessageExtensions
{

    public static SentryMessage ToSentryMessage(this Java.Protocol.Message message) =>
        new()
        {
            Message = message.GetMessage(),
            Formatted = message.Formatted,
            Params = message.Params
        };

    public static Java.Protocol.Message ToJavaMessage(this SentryMessage message)
    {
        var result = new Java.Protocol.Message
        {
            Formatted = message.Formatted,
            Params = message.Params?.Select(x => x.ToString() ?? "")?.ToList()
        };

        result.SetMessage(message.Message);

        return result;
    }
}
