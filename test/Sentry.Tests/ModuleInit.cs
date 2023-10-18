#if !__MOBILE__
public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
#if TRIMMABLE
        Verifier.DerivePathInfo((sourceFile, projectDirectory, type, method) => new(
            directory: projectDirectory,
            typeName: type.Name,
            // This ensures a unique name for our verify files when trimming is enabled
            methodName: method.Name + ".AOT"
            ));
#endif
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
