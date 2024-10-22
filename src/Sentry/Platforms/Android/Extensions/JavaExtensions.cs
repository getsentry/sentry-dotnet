using Android.OS;

namespace Sentry.Android.Extensions;

internal static class JavaExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this JavaDate timestamp) => DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Time);

    public static JavaDate ToJavaDate(this DateTimeOffset timestamp) => new(timestamp.ToUnixTimeMilliseconds());

    public static Exception ToException(this Throwable throwable) => Throwable.ToException(throwable);

    public static Throwable ToThrowable(this Exception exception) => Throwable.FromException(exception);

    public static IDictionary<TKey, TValue> WorkaroundKeyIteratorBug<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TKey,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TValue
        >
        (this IDictionary<TKey, TValue> dictionary)
    {
        // Workaround for https://github.com/getsentry/sentry-dotnet/issues/1751
        // Java.Lang.Error: no non-static method "Ljava/util/concurrent/ConcurrentHashMap$KeyIterator;.hasNext()Z"
        // Affects Android 9 ("Pie") and older (API 28)

        if (AndroidBuild.VERSION.SdkInt > BuildVersionCodes.P)
        {
            return dictionary;
        }

        var map = new JavaHashMap((IDictionary)dictionary);
        return new JavaDictionary<TKey, TValue>(map.Handle, JniHandleOwnership.DoNotRegister);
    }
}
