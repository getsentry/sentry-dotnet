using System;
using System.Linq;
using System.Reflection;

namespace Sentry.Tests.Protocol
{
    internal static class TypeExtensions
    {
        public static void AssertImmutable(this Type type)
        {
            if (type.IsPrimitive)
                return;
            if (type == typeof(string))
                return;
            if (type == typeof(DateTimeOffset))
                return;
            if (type == typeof(DateTime))
                return;
            if (type.IsEnum)
                return;
            if (type.Name.StartsWith("IImmutable"))
                return;

            var fieldInfos = type.GetFields(BindingFlags.Public
                                            | BindingFlags.NonPublic
                                            | BindingFlags.Instance);

            var notReadOnly = fieldInfos.Where(f => !f.IsInitOnly).ToList();
            if (notReadOnly.Any())
            {
                throw new Exception($"Type {type} has non readonly fields: " +
                                    string.Join(", ", notReadOnly.Select(p => p.Name)));
            }

            foreach (var fieldInfo in fieldInfos)
            {
                AssertImmutable(fieldInfo.FieldType);
            }
        }
    }
}
