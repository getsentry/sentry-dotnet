using System.Text;

namespace Sentry.AspNetCore.Extensions
{
    internal static class StringBuilderExtensions
    {
        internal static StringBuilder AppendIf(this StringBuilder builder, bool condition, string? value)
            => condition ? builder.Append(value) : builder;

        internal static StringBuilder AppendIf(this StringBuilder builder, bool condition, char value)
            => condition ? builder.Append(value) : builder;
    }
}
