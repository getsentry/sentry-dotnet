namespace Sentry.Android.Extensions;

internal static class DataCollectionExtensions
{
    public static void ApplyTo(this DataCollectionOptions options, JavaSdk.DataCollection native)
    {
        native.UserInfo = (JavaBoolean)options.UserInfo;
        native.Cookies = options.Cookies.ToJavaBehavior();
        native.UrlQueryParams = options.UrlQueryParams.ToJavaBehavior();
        native.HttpBodies = options.HttpBodies.ToJavaHttpBodyTypes();

        var headers = native.GetHttpHeaders();
        headers.Request = options.HttpHeaders.Request.ToJavaBehavior();
        headers.Response = options.HttpHeaders.Response.ToJavaBehavior();

        var graphql = native.GetGraphql();
        graphql.Document = (JavaBoolean)options.GraphQL.Document;
        graphql.Variables = (JavaBoolean)options.GraphQL.Variables;
    }

    private static JavaSdk.KeyValueCollectionBehavior ToJavaBehavior(this KeyValueCollectionBehavior behavior) =>
        behavior.Mode switch
        {
            KeyValueCollectionMode.Off => JavaSdk.KeyValueCollectionBehavior.Off(),
            KeyValueCollectionMode.DenyList => JavaSdk.KeyValueCollectionBehavior.DenyList([.. behavior.Terms]),
            KeyValueCollectionMode.AllowList => JavaSdk.KeyValueCollectionBehavior.AllowList([.. behavior.Terms]),
            _ => throw new ArgumentOutOfRangeException(nameof(behavior), behavior.Mode, message: default)
        };

    private static List<JavaSdk.HttpBodyType> ToJavaHttpBodyTypes(this HttpBodyTypes bodyTypes)
    {
        var result = new List<JavaSdk.HttpBodyType>();
        AddIfSet(HttpBodyTypes.IncomingRequest, JavaSdk.HttpBodyType.IncomingRequest);
        AddIfSet(HttpBodyTypes.OutgoingRequest, JavaSdk.HttpBodyType.OutgoingRequest);
        AddIfSet(HttpBodyTypes.IncomingResponse, JavaSdk.HttpBodyType.IncomingResponse);
        AddIfSet(HttpBodyTypes.OutgoingResponse, JavaSdk.HttpBodyType.OutgoingResponse);
        return result;

        void AddIfSet(HttpBodyTypes bodyType, JavaSdk.HttpBodyType javaBodyType)
        {
            if ((bodyTypes & bodyType) == bodyType)
            {
                result.Add(javaBodyType);
            }
        }
    }
}
