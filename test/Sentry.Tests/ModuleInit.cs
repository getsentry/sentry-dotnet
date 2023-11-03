#if !__MOBILE__
public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

        VerifierSettings.IgnoreMembers<SentryException>(_ => _.Module, _ => _.ThreadId);

        VerifierSettings.MemberConverter<Breadcrumb, IReadOnlyDictionary<string, string>>(
            target => target.Data,
            (_, value) =>
            {
                var dictionary = new Dictionary<string, string>();
                foreach (var pair in value)
                {
                    if (pair.Key == "stackTrace")
                    {
                        dictionary[pair.Key] = Scrubbers.ScrubStackTrace(pair.Value, true);
                    }
                    else
                    {
                        dictionary[pair.Key] = pair.Value.Replace('\\', '/');
                    }
                }

                return dictionary;
            });
    }
}
#endif
