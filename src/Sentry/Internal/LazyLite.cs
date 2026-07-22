namespace Sentry.Internal;

/// <summary>
/// A <see cref="ValueType"/>-based variant of <see cref="Lazy{T}"/> that is not thread safe.
///
/// We're using this for local variables, where no <see cref="LazyThreadSafetyMode"/> is required,
/// but a value is still cached, without any allocations via the <see langword="class"/>-based <see cref="Lazy{T}"/>.
/// </summary>
/// <remarks>
/// This type is not thread safe.
/// </remarks>
internal struct LazyLite<T>
{
    private Func<T>? _factory;
    private T? _value;

    public LazyLite(Func<T>? valueFactory)
    {
        _factory = valueFactory;
        _value = default;
    }

    public readonly bool IsValueCreated => _factory is null;

    public T Value => _factory is null ? _value! : CreateValue();

    private T CreateValue()
    {
        var factory = _factory;
        if (factory is not null)
        {
            _factory = null;
            _value = factory();
        }

        return _value!;
    }
}
