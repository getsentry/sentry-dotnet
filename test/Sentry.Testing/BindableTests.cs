#if !NETFRAMEWORK
using Microsoft.Extensions.Configuration;

namespace Sentry.Testing;

public abstract class BindableTests<TOptions>
{
    public class TextFixture
    {
        public IEnumerable<string> ExpectedPropertyNames => GetBindableProperties().Select(x => x.Name);
        public List<KeyValuePair<PropertyInfo, object>> ExpectedPropertyValues { get; }

        public IConfigurationRoot Config { get; }

        public TextFixture()
        {
            ExpectedPropertyValues = GetBindableProperties().Select(GetDummyBindableValue).ToList();
            Config = new ConfigurationBuilder()
                .AddInMemoryCollection(ExpectedPropertyValues.SelectMany(ToConfigValues))
                .Build();
        }
    }

    protected TextFixture Fixture { get; } = new();

    private static IEnumerable<PropertyInfo> GetBindableProperties()
    {
        return typeof(TOptions).GetProperties()
            .Where(p =>
                !p.PropertyType.IsSubclassOf(typeof(Delegate)) // Exclude delegate properties
                && !p.PropertyType.IsInterface // Exclude interface properties
                );
    }

    protected IEnumerable<string> GetPropertyNames<T>() => typeof(T).GetProperties().Select(x => x.Name).ToList();

    private static KeyValuePair<PropertyInfo,object> GetDummyBindableValue(PropertyInfo propertyInfo)
    {
        var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
        var value = propertyType switch
        {
            not null when propertyType == typeof(bool) => true,
            not null when propertyType == typeof(string) => $"fake {propertyInfo.Name}",
            not null when propertyType == typeof(int) => 7,
            not null when propertyType == typeof(long) => 7,
            not null when propertyType == typeof(float) => 0.3f,
            not null when propertyType == typeof(double) => 0.6,
            not null when propertyType == typeof(TimeSpan) => TimeSpan.FromSeconds(3),
            not null when propertyType.IsEnum => GetNonDefaultEnumValue(propertyType),
            not null when propertyType == typeof(Dictionary<string, string>) =>
                new Dictionary<string, string>
                {
                    {$"key1", $"{propertyInfo.Name}value1"},
                    {$"key2", $"{propertyInfo.Name}value2"}
                },
            _ => throw new NotSupportedException($"Unsupported property type on property {propertyInfo.Name}")
        };
        return new KeyValuePair<PropertyInfo,object>(propertyInfo, value);
    }

    private static IEnumerable<KeyValuePair<string, string>> ToConfigValues(KeyValuePair<PropertyInfo, object> item)
    {
        var (prop, value) = item;
        var propertyType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        if (propertyType == typeof(Dictionary<string, string>))
        {
            foreach (var kvp in (Dictionary<string, string>)value)
            {
                yield return new KeyValuePair<string, string>($"{prop.Name}:{kvp.Key}", kvp.Value);
            }
        }
        else
        {
            yield return new KeyValuePair<string, string>(prop.Name, value.ToString());
        }
    }

    private static object GetNonDefaultEnumValue(Type enumType)
    {
        var enumValues = Enum.GetValues(enumType);
        if (enumValues.Length > 1)
        {
            return enumValues.GetValue(1); // return second value
        }
        throw new InvalidOperationException("Enum has no non-default values");
    }

    protected void AssertContainsAllOptionsProperties(IEnumerable<string> actual)
    {
        var missing = Fixture.ExpectedPropertyNames.Where(x => !actual.Contains(x));

        missing.Should().BeEmpty();
    }

    protected void AssertContainsExpectedPropertyValues(TOptions actual)
    {
        using (new AssertionScope())
        {
            foreach (var (prop, expectedValue) in Fixture.ExpectedPropertyValues)
            {
                var actualValue = actual.GetProperty(prop.Name);
                if (prop.PropertyType == typeof(Dictionary<string, string>))
                {
                    actualValue.Should().BeEquivalentTo(expectedValue);
                }
                else
                {
                    actualValue.Should().Be(expectedValue);
                }
            }
        }
    }
}
#endif
