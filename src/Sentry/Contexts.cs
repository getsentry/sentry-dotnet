using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol;
using OperatingSystem = Sentry.Protocol.OperatingSystem;
using Trace = Sentry.Protocol.Trace;

namespace Sentry;

/// <summary>
/// Represents Sentry's structured Context.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/" />
public sealed class Contexts : ConcurrentDictionary<string, object>, IJsonSerializable
{
    /// <summary>
    /// Describes the application.
    /// </summary>
    public App App => this.GetOrCreate<App>(App.Type);

    /// <summary>
    /// Describes the browser.
    /// </summary>
    public Browser Browser => this.GetOrCreate<Browser>(Browser.Type);

    /// <summary>
    /// Describes the device.
    /// </summary>
    public Device Device => this.GetOrCreate<Device>(Device.Type);

    /// <summary>
    /// Defines the operating system.
    /// </summary>
    /// <remarks>
    /// In web contexts, this is the operating system of the browser (normally pulled from the User-Agent string).
    /// </remarks>
    public OperatingSystem OperatingSystem => this.GetOrCreate<OperatingSystem>(OperatingSystem.Type);

    /// <summary>
    /// Response interface that contains information on any HTTP response related to the event.
    /// </summary>
    public Response Response => this.GetOrCreate<Response>(Response.Type);

    /// <summary>
    /// This describes a runtime in more detail.
    /// </summary>
    public Runtime Runtime => this.GetOrCreate<Runtime>(Runtime.Type);

    /// <summary>
    /// This describes a GPU of the device.
    /// </summary>
    public Gpu Gpu => this.GetOrCreate<Gpu>(Gpu.Type);

    /// <summary>
    /// This describes trace information.
    /// </summary>
    public Trace Trace => this.GetOrCreate<Trace>(Trace.Type);

    /// <summary>
    /// Initializes an instance of <see cref="Contexts"/>.
    /// </summary>
    public Contexts() : base(StringComparer.Ordinal) { }

    /// <summary>
    /// Creates a deep clone of this context.
    /// </summary>
    internal Contexts Clone()
    {
        var context = new Contexts();

        CopyTo(context);

        return context;
    }

    /// <summary>
    /// Copies the items of the context while cloning the known types.
    /// </summary>
    internal void CopyTo(Contexts to)
    {
        foreach (var kv in this)
        {
            to.AddOrUpdate(kv.Key,

                addValueFactory: _ =>
                    kv.Value is ICloneable<object> cloneable
                        ? cloneable.Clone()
                        : kv.Value,

                updateValueFactory: (_, existing) =>
                {
                    if (existing is IUpdatable updatable)
                    {
                        updatable.UpdateFrom(kv.Value);
                    }
                    else if (kv.Value is IDictionary<string, object?> source &&
                             existing is IDictionary<string, object?> target)
                    {
                        foreach (var item in source)
                        {
                            if (!target.TryGetValue(item.Key, out var value))
                            {
                                target.Add(item);
                            }
                            else if (value is null)
                            {
                                target[item.Key] = item.Value;
                            }
                        }
                    }

                    return existing;
                });
        }
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        var contexts = this.OrderBy(x => x.Key, StringComparer.Ordinal);
        writer.WriteDictionaryValue(contexts!, logger, includeNullValues: false);
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Contexts FromJson(JsonElement json)
    {
        var result = new Contexts();

        foreach (var (name, value) in json.EnumerateObject())
        {
            var type = value.GetPropertyOrNull("type")?.GetString() ?? name;

            // Handle known context types
            if (string.Equals(type, App.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = App.FromJson(value);
            }
            else if (string.Equals(type, Browser.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = Browser.FromJson(value);
            }
            else if (string.Equals(type, Device.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = Device.FromJson(value);
            }
            else if (string.Equals(type, OperatingSystem.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = OperatingSystem.FromJson(value);
            }
            else if (string.Equals(type, Response.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = Response.FromJson(value);
            }
            else if (string.Equals(type, Runtime.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = Runtime.FromJson(value);
            }
            else if (string.Equals(type, Gpu.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = Gpu.FromJson(value);
            }
            else if (string.Equals(type, Trace.Type, StringComparison.OrdinalIgnoreCase))
            {
                result[name] = Trace.FromJson(value);
            }
            else
            {
                // Unknown context - parse as dictionary
                var dynamicContext = value.GetDynamicOrNull();
                if (dynamicContext is not null)
                {
                    result[name] = dynamicContext;
                }
            }
        }

        return result;
    }

    internal void ReplaceWith(Contexts? contexts)
    {
        Clear();

        if (contexts == null)
        {
            return;
        }

        foreach (var context in contexts)
        {
            this[context.Key] = context.Value;
        }
    }

    internal Contexts? NullIfEmpty() => IsEmpty ? null : this;
}
