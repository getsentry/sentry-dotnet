namespace Sentry.Testing;

public static class ReflectionExtensions
{
    /// <summary>
    /// Raises an event.
    /// </summary>
    /// <param name="source">The source of the event..</param>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="eventArgs">The arguments to pass to the event handler.</param>
    public static void RaiseEvent(this object source, string eventName, object eventArgs)
    {
        var delegateField = source.GetType().FindField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
        var eventDelegate = delegateField?.GetValue(source) as Delegate;
        eventDelegate?.DynamicInvoke(source, eventArgs);
    }

    private static FieldInfo FindField(this Type type, string name, BindingFlags bindingFlags)
    {
        while (true)
        {
            var field = type.GetField(name, bindingFlags);
            if (field != null)
            {
                return field;
            }

            var baseType = type.BaseType;
            if (baseType == null)
            {
                return null;
            }

            type = baseType;
        }
    }
}
