using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace Sentry.Serilog.Tests
{
    // As borrowed from: https://github.com/serilog/serilog-settings-configuration/blob/dev/src/Serilog.Settings.Configuration/Settings/Configuration/StringArgumentValue.cs
    // and: https://github.com/serilog/serilog-settings-configuration/blob/dev/src/Serilog.Settings.Configuration/Settings/Configuration/ConfigurationReader.cs
    // Serilog License: https://github.com/serilog/serilog-settings-configuration/blob/dev/LICENSE
    public static class SerilogStringArgumentValue
    {
        public static bool HasImplicitValueWhenNotSpecified(ParameterInfo paramInfo)
        {
            return paramInfo.HasDefaultValue
                   // parameters of type IConfiguration are implicitly populated with provided Configuration
                   || paramInfo.ParameterType == typeof(IConfiguration);
        }

        public static object GetImplicitValueForNotSpecifiedKey(ParameterInfo parameter, MethodInfo methodToInvoke)
        {
            if (!HasImplicitValueWhenNotSpecified(parameter))
            {
                throw new InvalidOperationException("GetImplicitValueForNotSpecifiedKey() should only be called for parameters for which HasImplicitValueWhenNotSpecified() is true. " +
                                                    "This means something is wrong in the Serilog.Settings.Configuration code.");
            }

            if (parameter.ParameterType == typeof(IConfiguration))
            {
                if (parameter.HasDefaultValue)
                {
                    return parameter.DefaultValue;
                }

                throw new InvalidOperationException("Trying to invoke a configuration method accepting a `IConfiguration` argument. " +
                                                    $"This is not supported when only a `IConfigSection` has been provided. (method '{methodToInvoke}')");
            }

            return parameter.DefaultValue;
        }

        public static bool ParameterNameMatches(string actualParameterName, string suppliedName)
        {
            return suppliedName.Equals(actualParameterName, StringComparison.OrdinalIgnoreCase);
        }

        public static readonly Dictionary<Type, Func<string, object>> ExtendedTypeConversions = new Dictionary<Type, Func<string, object>>
        {
            { typeof(Uri), s => new Uri(s) },
            { typeof(TimeSpan), s => TimeSpan.Parse(s) },
            { typeof(Type), s => Type.GetType(s, throwOnError:true) },
            { typeof(Dsn), s => new Dsn(s) }
        };

        public static object ConvertTo(string argumentValue, Type toType)
        {

            var toTypeInfo = toType.GetTypeInfo();
            if (toTypeInfo.IsGenericType && toType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (string.IsNullOrEmpty(argumentValue))
                    return null;

                // unwrap Nullable<> type since we're not handling null situations
                toType = toTypeInfo.GenericTypeArguments[0];
                toTypeInfo = toType.GetTypeInfo();
            }

            if (toTypeInfo.IsEnum)
                return Enum.Parse(toType, argumentValue);

            var convertor = ExtendedTypeConversions
                .Where(t => t.Key.GetTypeInfo().IsAssignableFrom(toTypeInfo))
                .Select(t => t.Value)
                .FirstOrDefault();

            if (convertor != null)
                return convertor(argumentValue);

            if ((toTypeInfo.IsInterface || toTypeInfo.IsAbstract) && !string.IsNullOrWhiteSpace(argumentValue))
            {
                //check if value looks like a static property or field directive
                // like "Namespace.TypeName::StaticProperty, AssemblyName"
                if (TryParseStaticMemberAccessor(argumentValue, out var accessorTypeName, out var memberName))
                {
                    var accessorType = Type.GetType(accessorTypeName, throwOnError: true);
                    // is there a public static property with that name ?
                    var publicStaticPropertyInfo = accessorType.GetTypeInfo().DeclaredProperties
                        .Where(x => x.Name == memberName)
                        .Where(x => x.GetMethod != null)
                        .Where(x => x.GetMethod.IsPublic)
                        .FirstOrDefault(x => x.GetMethod.IsStatic);

                    if (publicStaticPropertyInfo != null)
                    {
                        return publicStaticPropertyInfo.GetValue(null); // static property, no instance to pass
                    }

                    // no property ? look for a public static field
                    var publicStaticFieldInfo = accessorType.GetTypeInfo().DeclaredFields
                        .Where(x => x.Name == memberName)
                        .Where(x => x.IsPublic)
                        .FirstOrDefault(x => x.IsStatic);

                    if (publicStaticFieldInfo != null)
                    {
                        return publicStaticFieldInfo.GetValue(null); // static field, no instance to pass
                    }

                    throw new InvalidOperationException($"Could not find a public static property or field with name `{memberName}` on type `{accessorTypeName}`");
                }

                // maybe it's the assembly-qualified type name of a concrete implementation
                // with a default constructor
                var type = FindType(argumentValue.Trim());
                if (type != null)
                {
                    var ctor = type.GetTypeInfo().DeclaredConstructors.FirstOrDefault(ci =>
                    {
                        var parameters = ci.GetParameters();
                        return parameters.Length == 0 || parameters.All(pi => pi.HasDefaultValue);
                    });

                    if (ctor == null)
                        throw new InvalidOperationException($"A default constructor was not found on {type.FullName}.");

                    var call = ctor.GetParameters().Select(pi => pi.DefaultValue).ToArray();
                    return ctor.Invoke(call);
                }
            }

            return Convert.ChangeType(argumentValue, toType);
        }

        public static Type FindType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null)
            {
                if (!typeName.Contains(','))
                {
                    type = Type.GetType($"{typeName}, Serilog");
                }
            }

            return type;
        }

        static readonly Regex StaticMemberAccessorRegex = new Regex("^(?<shortTypeName>[^:]+)::(?<memberName>[A-Za-z][A-Za-z0-9]*)(?<typeNameExtraQualifiers>[^:]*)$");

        public static bool TryParseStaticMemberAccessor(string input, out string accessorTypeName, out string memberName)
        {
            if (input == null)
            {
                accessorTypeName = null;
                memberName = null;
                return false;
            }
            if (StaticMemberAccessorRegex.IsMatch(input))
            {
                var match = StaticMemberAccessorRegex.Match(input);
                var shortAccessorTypeName = match.Groups["shortTypeName"].Value;
                var rawMemberName = match.Groups["memberName"].Value;
                var extraQualifiers = match.Groups["typeNameExtraQualifiers"].Value;

                memberName = rawMemberName.Trim();
                accessorTypeName = shortAccessorTypeName.Trim() + extraQualifiers.TrimEnd();
                return true;
            }
            accessorTypeName = null;
            memberName = null;
            return false;
        }
    }
}
