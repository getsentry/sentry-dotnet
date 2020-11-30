using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sentry.Protocol;
using VerifyTests;

namespace Sentry.Tests
{
    public static class ModuleInit
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifierSettings.ModifySerialization(
                settings => settings.MemberConverter<Breadcrumb, IReadOnlyDictionary<string, string>>(
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
                                dictionary[pair.Key] = pair.Value.Replace('\\','/');
                            }
                        }
                        return dictionary;
                    }));
        }
    }
}
