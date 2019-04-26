using System;

using NLog.Config;

using NSubstitute;

using Sentry.Infrastructure;

using Xunit;

namespace Sentry.NLog.Tests
{
    public class SentryTargetTests
    {
 
        const string TestDsn = "http://9a665b8d28bd4cc085e2c6c0fc7198e5:355a4774acaa4a7f8cde281c40ddbba7@myservice.myserver.com:9000/1";
        [Fact]
        public void Can_configure_from_xml_file()
        {
            try
            {
                string configXml = $@"
                <nlog throwConfigExceptions='true'>
                    <extensions>
                        <add type='{typeof(SentryTarget).AssemblyQualifiedName}' />
                    </extensions>
                    <targets>
                        <target type='Sentry' name='sentry' dsn='{TestDsn}'>
                            <options>
                                <environment>Development</environment>
                            </options>                
                        </target>
                    </targets>
                </nlog>";

                var c = XmlLoggingConfiguration.CreateFromXmlString(configXml);

                var t = c.FindTargetByName("sentry") as SentryTarget;
                Assert.NotNull(t);
                Assert.Equal(TestDsn, t.Options.Dsn.ToString());
                Assert.Equal("Development", t.Options.Environment);
            }
            catch (Exception ex)
            {
                _ = ex;

                throw;
            }
        }

    }
}
