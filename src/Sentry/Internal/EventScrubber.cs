namespace Sentry.Internal;

using Sentry;
using System.Collections.Generic;

internal class EventScrubber(IList<string> denylist)
{
    private IList<string> Denylist { get; } = denylist;
    internal const string ScrubbedText = "*****";
    internal static readonly string[] DefaultDenylist =
    [
        // stolen from relay
        "password", "passwd", "secret", "api_key", "apikey", "auth", "credentials",
        "mysql_pwd", "privatekey", "private_key", "token", "ip_address", "session",
        // common names used in web frameworks
        "csrftoken", "sessionid",
        "remote_addr", "x_csrftoken", "x_forwarded_for", "set_cookie", "cookie",
        "authorization", "x_api_key", "x_forwarded_for", "x_real_ip",
        "aiohttp_session", "connect.sid", "csrf_token", "csrf", "_csrf", "_csrf_token",
        "PHPSESSID", "_session", "symfony", "user_session", "_xsrf", "XSRF-TOKEN"
    ];

    public EventScrubber() : this(DefaultDenylist)
    {
    }

    private void ScrubStringDictionary(IDictionary<string, string> dict)
    {
        foreach (var key in dict.Keys.ToList())
        {
            if (Denylist.Contains(key, StringComparer.InvariantCultureIgnoreCase))
            {
                dict[key] = ScrubbedText;
            }
        }
    }

    private void ScrubRequest(SentryEvent ev)
    {
        ScrubStringDictionary(ev.Request.Headers);

        if (ev.Request.Cookies != null)
        {
            var cookies = ev.Request.Cookies.Split(';');
            foreach (var cookie in cookies)
            {
                var parts = cookie.Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }

                var name = parts[0].Trim();
                if (Denylist.Contains(name, StringComparer.InvariantCultureIgnoreCase))
                {
                    ev.Request.Cookies = ev.Request.Cookies.Replace(cookie, $"{name}={ScrubbedText}");
                }
            }
        }
    }

    public void ScrubExtra(SentryEvent ev)
    {
        foreach (var key in ev.Extra.Keys.ToList())
        {
            if (Denylist.Contains(key, StringComparer.InvariantCultureIgnoreCase))
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

            foreach (var key in data.Keys.ToList())
            {
                if (Denylist.Contains(key, StringComparer.InvariantCultureIgnoreCase))
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

    public void ScrubEvent(SentryEvent ev)
    {
        ScrubRequest(ev);
        ScrubExtra(ev);
        ScrubUser(ev);
        ScrubBreadcrumbs(ev);
        ScrubFrames(ev);
    }
}
