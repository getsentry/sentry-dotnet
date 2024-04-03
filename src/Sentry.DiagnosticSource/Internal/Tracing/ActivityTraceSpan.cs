namespace Sentry.Internal.Tracing;

#if !NET6_0_OR_GREATER
using System.Diagnostics;
#endif

/// <summary>
/// Wraps the functionality that we use from Activity in an interface so that we can
/// access this from our integrations without taking a hard dependency on
/// System.Diagnostics.Activity (which is only available in .NET 5.0 and later)
/// </summary>
internal class ActivityTraceSpan(System.Diagnostics.Activity activity) : ITraceSpan
{
    public string? Description => activity.DisplayName;

    public ITraceSpan AddEvent(string message)
    {
        activity.AddEvent(new ActivityEvent(message));
        return this;
    }

    public ITraceSpan SetAttribute(string key, object value)
    {
        activity.SetTag(key, value);
        return this;
    }

    public ITraceSpan SetDescription(string? description)
    {
        if (description != null)
        {
            activity.DisplayName = description;
        }
        return this;
    }

    public ITraceSpan SetStatus(SpanStatus status, string? description = default)
    {
        if (status == SpanStatus.Ok)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
            return this;
        }
        var errorMessage = description ?? status.ToString();
        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        return this;
    }

    public ITraceSpan Stop()
    {
        activity.Stop();
        return this;
    }

    public ITraceSpan SetExtra(string key, object? value)
    {
        // TODO: Not sure what we want to do about Extra. This would change that data from being stored as "Extra" data
        // (which is documented as being obsolete) to being stored as tags... which is a change in behaviour. The docs
        // say structured contexts should be used instead, so maybe we need to create some new structured contexts for
        // this "arbitrary" extra data that both we and users are currently storing. Alternatively, we can continue to
        // store it as extra (in which case we need to implement this in some kind of custom property both here and when
        // converting the activity into a SentrySpan for transmission)
        activity.SetTag(key, value);
        return this;
    }

    public ITraceSpan Finish(Exception exception)
    {
        activity.BindException(exception);
        return Stop();
    }

    public void Dispose() => activity.Dispose();
}
