using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Sentry.Testing;
using Serilog;
using Xunit;

namespace Sentry.Serilog.Tests
{
    public class SentrySerilogConfigurationSettingsIntegrationTests
    {
        private class Fixture
        {
            public SentrySerilogOptions Options { get; } = new SentrySerilogOptions();
        }

        private readonly Fixture _fixture = new Fixture();

        [Theory]
        [InlineData(
            "All items configured",
            @"{
                ""Serilog"": {
                    ""Using"": [
                        ""Serilog"",
                        ""Sentry""
                    ],
                    ""WriteTo"": [{
                            ""Name"": ""Sentry"",
                            ""Args"": {
                                ""sendDefaultPii"": false,
                                ""isEnvironmentUser"": false,
                                ""serverName"": ""MyServerName"",
                                ""attachStackTrace"": false,
                                ""maxBreadcrumbs"": 20,
                                ""sampleRate"": 0.5,
                                ""release"": ""0.0.1"",
                                ""environment"": ""staging"",
                                ""dsn"": ""https://MY-DSN@sentry.io/0"",
                                ""maxQueueItems"": 100,
                                ""shutdownTimeout"": ""00:00:05"",
                                ""decompressionMethods"": ""GZip"",
                                ""requestBodyCompressionLevel"": ""NoCompression"",
                                ""requestBodyCompressionBuffered"": false,
                                ""debug"": false,
                                ""diagnosticsLevel"": ""Debug"",
                                ""reportAssemblies"": false,
                                ""deduplicateMode"": ""All"",
                                ""initializeSdk"": true,
                                ""minimumBreadcrumbLevel"": ""Verbose"",
                                ""minimumEventLevel"": ""Error""
                            }
                        }
                    ]
                }
            }")]
        [InlineData(
            "No properties are provided",
            @"{
                ""Serilog"": {
                    ""Using"": [
                        ""Serilog"",
                        ""Sentry""
                    ],
                    ""WriteTo"": [{
                            ""Name"": ""Sentry""
                        }
                    ]
                }
            }")]
        [InlineData(
            "Some properties are provided",
            @"{
                ""Serilog"": {
                    ""Using"": [
                        ""Serilog"",
                        ""Sentry""
                    ],
                    ""WriteTo"": [{
                            ""Name"": ""Sentry"",
                            ""Args"": {
                                ""sendDefaultPii"": false,
                                ""isEnvironmentUser"": false
                            }
                        }
                    ]
                }
            }")]
        [InlineData(
            "Properties are provided out of order",
            @"{
                ""Serilog"": {
                    ""Using"": [
                        ""Serilog"",
                        ""Sentry""
                    ],
                    ""WriteTo"": [{
                            ""Name"": ""Sentry"",
                            ""Args"": {
                                ""release"": ""0.0.1-pre"",
                                ""minimumBreadcrumbLevel"": ""Verbose"",
                                ""reportAssemblies"": false,
                                ""isEnvironmentUser"": false,
                                ""attachStackTrace"": false,
                                ""shutdownTimeout"": ""00:00:09"",
                                ""environment"": ""some environment""
                            }
                        }
                    ]
                }
            }")]
        public void SentrySerilogOptionsIsConfiguredFromJsonProperly(string when, string json)
        {
            Assert.False(string.IsNullOrWhiteSpace(when));

            // This is used as the resulting object / output of the method being tested
            var actualSentrySerilogOptions = new SentrySerilogOptions();

            var configurationSections = GetConfigurationSectionsFromJson(json);
            var methodInfo = GetTestMethodInfo();
            var callParameters = GetCallParameters(methodInfo, configurationSections, actualSentrySerilogOptions);
            
            // Make the actual call to configure the object
            methodInfo.Invoke(null, callParameters.ToArray());

            // Validate that the properties passed via the Theory do indeed line up, and the ones
            // that weren't provided are set to the expected default value.
            foreach (var propertyInfo in _fixture.Options.GetType().GetProperties())
            {
                // Attempt to find the property in the provided json
                var configurationItem = configurationSections.FirstOrDefault(ci =>
                    ci.Key.Equals(propertyInfo.Name, StringComparison.OrdinalIgnoreCase));

                var fixturePropertyValue = propertyInfo.GetValue(_fixture.Options);
                var actualPropertyValue = propertyInfo.GetValue(actualSentrySerilogOptions);

                // Compare strings to strings
                if (propertyInfo.PropertyType.IsAssignableFrom(typeof(string)))
                {
                    Assert.Equal(configurationItem?.Value ?? fixturePropertyValue, actualPropertyValue);
                }
                else if (propertyInfo.PropertyType.IsClass)
                {
                    // Deep equals for classes
                    var leftObject =
                        configurationItem != null
                            ? SerilogStringArgumentValue.ConvertTo(configurationItem.Value, propertyInfo.PropertyType)
                            : fixturePropertyValue;
                    var serializedLeftObject = JsonConvert.SerializeObject(leftObject);
                    var serializedActualObject = JsonConvert.SerializeObject(actualPropertyValue);
                    Assert.Equal(serializedLeftObject, serializedActualObject);
                }
                else
                {
                    // Re-use the Serilog-borrowed code to convert the json string value into the
                    // expected type if it was provided.
                    Assert.Equal(
                        configurationItem != null
                            ? SerilogStringArgumentValue.ConvertTo(configurationItem.Value, propertyInfo.PropertyType)
                            : fixturePropertyValue,
                        actualPropertyValue
                        );
                }
            }
        }

        private static List<IConfigurationSection> GetConfigurationSectionsFromJson(string json)
        {
            // Setup the configuration that Serilog would otherwise build for us
            var configurationBuilder = new ConfigurationBuilder().AddJsonString(json);
            IConfiguration configurationRoot = configurationBuilder.Build();

            // The provided json is in the expected format for Serilog to avoid confusion / any
            // potential copy/pasting of this code into a json file which wouldn't work.
            var configurationSection = configurationRoot.GetSection("Serilog:WriteTo:0:Args");
            return configurationSection.GetChildren().ToList();
        }

        private static MethodInfo GetTestMethodInfo()
        {
            // Find the method that is being tested
            var extensionClassType = typeof(SentrySinkExtensions);
            var methods = extensionClassType.GetMethods();
            return methods.First(mi => mi.Name.Equals(nameof(SentrySinkExtensions.ConfigureSentrySerilogOptions), StringComparison.Ordinal));
        }

        private static List<object> GetCallParameters(MethodInfo methodInfo, List<IConfigurationSection> configurationItems,
            SentrySerilogOptions sentrySerilogOptions)
        {
            // Configure the parameters to the function based on the theory being passed to the test.
            // Note that the first item is the actual SentrySerilogOptions class that gets configured
            // as part of the call. Also note that this code was borrowed from the Serilog settings
            // configuration project.
            var callParameters = (from p in methodInfo.GetParameters().Skip(1)
                let directive = configurationItems.FirstOrDefault(s => SerilogStringArgumentValue.ParameterNameMatches(p.Name, s.Key))
                select directive?.Key == null
                    ? SerilogStringArgumentValue.GetImplicitValueForNotSpecifiedKey(p, methodInfo)
                    : SerilogStringArgumentValue.ConvertTo(directive.Value, p.ParameterType)).ToList();

            // Insert our resulting-object as the first parameter
            callParameters.Insert(0, sentrySerilogOptions);

            return callParameters;
        }
    }
}
