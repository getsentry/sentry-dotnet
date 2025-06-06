namespace Sentry.Maui.Internal;

internal static class Extensions
{
    public static void AddBreadcrumbForEvent(this IHub hub,
        SentryMauiOptions options,
        object? sender,
        string eventName,
        string? type,
        string? category,
        Action<Dictionary<string, string>>? addExtraData)
        => hub.AddBreadcrumbForEvent(options, sender, eventName, type, category, default, addExtraData);

    public static void AddBreadcrumbForEvent(this IHub hub,
        SentryMauiOptions options,
        object? sender,
        string eventName,
        string? type = null,
        string? category = null,
        BreadcrumbLevel level = default,
        Action<Dictionary<string, string>>? addExtraData = null)
    {
        var data = new Dictionary<string, string>();
        if (sender is Element element)
        {
            data.AddElementInfo(options, element, null);
        }

        addExtraData?.Invoke(data);

        var message = sender != null ? $"{sender.ToStringOrTypeName()}.{eventName}" : eventName;
        hub.AddBreadcrumb(message, category, type, data, level);
    }

    public static void AddElementInfo(this IDictionary<string, string> data,
        SentryMauiOptions options,
        Element? element,
        string? property)
    {
        if (element is null)
        {
            return;
        }

        var typeName = element.ToStringOrTypeName();
        var prefix = (property ?? typeName) + ".";

        if (property != null)
        {
            data.Add(property, typeName);
        }

        // The element ID seems to be mostly useless noise
        //data.Add(prefix + nameof(element.Id), element.Id.ToString());

        if (element.AutomationId is { } automationId)
        {
            data.Add(prefix + nameof(Element.AutomationId), automationId);
        }

        if (element.StyleId is { } styleId)
        {
            // The StyleId correlates to the element's name if one is set in XAML
            // TODO: Is there a better way to get this?
            data.Add(prefix + "Name", styleId);
        }

        if (options.IncludeTitleInBreadcrumbs && element is ITitledElement { Title: { } title })
        {
            data.Add(prefix + nameof(ITitledElement.Title), title);
        }

        if (options.IncludeTextInBreadcrumbs && element is IText { Text: { } text })
        {
            data.Add(prefix + nameof(IText.Text), text);
        }
    }

    public static string ToStringOrTypeName(this object o)
    {
        var t = o.GetType();
        var s = o.ToString();
        return s == null || s == t.FullName ? t.Name : s;
    }
}
