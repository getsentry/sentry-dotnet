namespace Sentry.Internal;

using System.Collections.Generic;
using Sentry;

internal class EventScrubber
{
    internal HashSet<string> Denylist { get; }
    internal const string ScrubbedText = "*****";
    internal static readonly string[] DefaultDenylist =
    [
        // stolen from relay
        "password",
        "passwd",
        "secret",
        "api_key",
        "apikey",
        "auth",
        "credentials",
        "mysql_pwd",
        "privatekey",
        "private_key",
        "token",
        "ip_address",
        "session",
        // common names used in web frameworks
        "csrftoken",
        "sessionid",
        "remote_addr",
        "x_csrftoken",
        "x_forwarded_for",
        "set_cookie",
        "cookie",
        "authorization",
        "x_api_key",
        "x_forwarded_for",
        "x_real_ip",
        "aiohttp_session",
        "connect.sid",
        "csrf_token",
        "csrf",
        "_csrf",
        "_csrf_token",
        "PHPSESSID",
        "_session",
        "symfony",
        "user_session",
        "_xsrf",
        "XSRF-TOKEN"
    ];

    public EventScrubber()
    {
        Denylist = new(DefaultDenylist, StringComparer.InvariantCultureIgnoreCase);
    }

    internal EventScrubber(IEnumerable<string> denylist)
    {
        Denylist = new(denylist, StringComparer.InvariantCultureIgnoreCase);
    }

    private void ScrubStringDictionary(IDictionary<string, string> dict)
    {
#if NETFRAMEWORK
        foreach (var key in dict.Keys.ToArray())
#else
        foreach (var key in dict.Keys)
#endif
        {
            if (Denylist.Contains(key))
            {
                dict[key] = ScrubbedText;
            }
        }
    }

    private void ScrubRequest(SentryEvent ev)
    {
        ScrubStringDictionary(ev.Request.Headers);

        if (ev.Request.Cookies == null)
        {
            return;
        }

        var cookies = ev.Request.Cookies.Split(';');
        foreach (var cookie in cookies)
        {
            var parts = cookie.Split('=');
            if (parts.Length != 2)
            {
                continue;
            }

            var name = parts[0].Trim();
            if (Denylist.Contains(name))
            {
                ev.Request.Cookies = ev.Request.Cookies.Replace(cookie, $"{name}={ScrubbedText}");
            }
        }
    }

    private void ScrubExtra(SentryEvent ev)
    {
#if NETFRAMEWORK
        foreach (var key in ev.Extra.Keys.ToArray())
#else
        foreach (var key in ev.Extra.Keys)
#endif
        {
            if (Denylist.Contains(key))
            {
                ev.SetExtra(key, ScrubbedText);
            }
        }
    }

    private void ScrubUser(SentryEvent ev)
    {
        ScrubStringDictionary(ev.User.Other);
    }

    private void ScrubBreadcrumbs(SentryEvent ev)
    {
        foreach (var breadcrumb in ev.Breadcrumbs)
        {
            if (breadcrumb.Data is not { } data)
            {
                continue;
            }

#if NETFRAMEWORK
            foreach (var key in data.Keys.ToArray())
#else
            foreach (var key in data.Keys)
#endif
            {
                if (Denylist.Contains(key))
                {
                    breadcrumb.ScrubData(key, ScrubbedText);
                }
            }
        }
    }

    private void ScrubFrames(SentryEvent ev)
    {
        if (ev.SentryExceptions == null)
        {
            return;
        }

        foreach (var exception in ev.SentryExceptions)
        {
            if (exception.Stacktrace == null)
            {
                continue;
            }

            foreach (var frame in exception.Stacktrace.Frames)
            {
                ScrubStringDictionary(frame.Vars);
            }
        }
    }

    public virtual void ScrubEvent(SentryEvent ev)
    {
        ScrubRequest(ev);
        ScrubExtra(ev);
        ScrubUser(ev);
        ScrubBreadcrumbs(ev);
        ScrubFrames(ev);
    }
}
