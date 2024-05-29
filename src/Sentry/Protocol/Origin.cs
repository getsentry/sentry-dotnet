namespace Sentry.Protocol;

/// <summary>
/// Trace origin indicates what created a trace or a span.
/// </summary>
public readonly struct Origin
{
    private readonly string? _category;
    private readonly string? _integrationName;
    private readonly string? _integrationPart;

    /// <summary>
    ///  Can be either manual (user created the trace/span) or auto (created by SDK/integration)
    /// </summary>
    internal OriginType? Type { get; init; }

    /// <summary>
    /// The category of the trace or span.
    /// See https://develop.sentry.dev/sdk/performance/span-operations/#currently-used-categories
    /// </summary>
    internal string? Category
    {
        get => _category;
        init
        {
            if (OriginValidator.IsValidPartName(value))
            {
                throw new ArgumentException("Invalid part name", nameof(Category));
            }
            _category = value;
        }
    }

    /// <summary>
    /// The name of the integration or the SDK that created the trace or span.
    /// </summary>
    internal string? IntegrationName
    {
        get => _integrationName;
        init
        {
            if (OriginValidator.IsValidPartName(value))
            {
                throw new ArgumentException("Invalid part name", nameof(IntegrationName));
            }
            _integrationName = value;
        }
    }

    /// <summary>
    /// The part of the integration of the SDK that created the trace or span.
    /// </summary>
    internal string? IntegrationPart
    {
        get => _integrationPart;
        init
        {
            if (OriginValidator.IsValidPartName(value))
            {
                throw new ArgumentException("Invalid part name", nameof(IntegrationPart));
            }
            _integrationPart = value;
        }
    }

    /// <summary>
    /// The origin is of type string and consists of four parts:
    ///   {type}.{category}.{integration-name}.{integration-part}
    /// Only the first is mandatory. The parts build upon each other, meaning it is forbidden to skip one part.
    /// For example, you may send parts one and two but aren't allowed to send parts one and three without part two.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        char? separator = null;
        var builder = new StringBuilder();
        foreach (var part in new [] {
             Type?.ToString()?.ToLower(),
             Category,
             IntegrationName,
             IntegrationPart
        })
        {
            if (string.IsNullOrEmpty(part))
            {
                break;
            }

            if (separator is not null)
            {
                builder.Append(separator);
            }
            builder.Append(part);

            // Ensure separator for subsequent items
            separator ??= '.';
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Prime number picked out of a hat
        const int hashCodeBase = 523;
        return hashCodeBase
               + Type.GetHashCode()
               + (Category ?? string.Empty).GetHashCode()
               + (IntegrationName ?? string.Empty).GetHashCode()
               + (IntegrationPart ?? string.Empty).GetHashCode();
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Origin otherOrigin && otherOrigin.GetHashCode() == GetHashCode();
    }
}
