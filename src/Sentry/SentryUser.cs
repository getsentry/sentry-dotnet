using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// An interface which describes the authenticated User for a request.
/// </summary>
/// <see href="https://develop.sentry.dev/sdk/event-payloads/user/"/>
public sealed class SentryUser : IJsonSerializable
{
    internal Action<SentryUser>? PropertyChanged { get; set; }

    private string? _id;
    private string? _username;
    private string? _email;
    private string? _ipAddress;
    private string? _segment;
    private IDictionary<string, string>? _other;

    /// <summary>
    /// The unique ID of the user.
    /// </summary>
    public string? Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// The username of the user.
    /// </summary>
    public string? Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// The email address of the user.
    /// </summary>
    public string? Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// The IP address of the user.
    /// </summary>
    public string? IpAddress
    {
        get => _ipAddress;
        set
        {
            if (_ipAddress != value)
            {
                _ipAddress = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// The segment the user belongs to.
    /// </summary>
    public string? Segment
    {
        get => _segment;
        set
        {
            if (_segment != value)
            {
                _segment = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// Additional information about the user.
    /// </summary>
    public IDictionary<string, string> Other
    {
        get => _other ??= new Dictionary<string, string>();
        set
        {
            if (_other != value)
            {
                _other = value;
                PropertyChanged?.Invoke(this);
            }
        }
    }

    /// <summary>
    /// Clones the current <see cref="SentryUser"/> instance.
    /// </summary>
    /// <returns>The cloned user.</returns>
    public SentryUser Clone()
    {
        var user = new SentryUser();
        CopyTo(user);
        return user;
    }

    internal void CopyTo(SentryUser? user)
    {
        if (user == null)
        {
            return;
        }

        user.Id ??= Id;
        user.Username ??= Username;
        user.Email ??= Email;
        user.IpAddress ??= IpAddress;
        user.Segment ??= Segment;

        user._other ??= _other?.ToDictionary(
            entry => entry.Key,
            entry => entry.Value);
    }

    internal bool HasAnyData() =>
        Id is not null ||
        Username is not null ||
        Email is not null ||
        IpAddress is not null ||
        Segment is not null ||
        _other?.Count > 0;

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? _)
    {
        writer.WriteStartObject();

        writer.WriteStringIfNotWhiteSpace("id", Id);
        writer.WriteStringIfNotWhiteSpace("username", Username);
        writer.WriteStringIfNotWhiteSpace("email", Email);
        writer.WriteStringIfNotWhiteSpace("ip_address", IpAddress);
        writer.WriteStringIfNotWhiteSpace("segment", Segment);
        writer.WriteStringDictionaryIfNotEmpty("other", _other!);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryUser FromJson(JsonElement json)
    {
        var id = json.GetPropertyOrNull("id")?.GetString();
        var username = json.GetPropertyOrNull("username")?.GetString();
        var email = json.GetPropertyOrNull("email")?.GetString();
        var ip = json.GetPropertyOrNull("ip_address")?.GetString();
        var segment = json.GetPropertyOrNull("segment")?.GetString();
        var other = json.GetPropertyOrNull("other")?.GetStringDictionaryOrNull();

        return new SentryUser
        {
            Id = id,
            Username = username,
            Email = email,
            IpAddress = ip,
            Segment = segment,
            _other = other?.WhereNotNullValue().ToDict()
        };
    }
}
